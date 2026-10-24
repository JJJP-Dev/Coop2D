using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The elemental aura on an enemy. The host decides it and every client shows it.
[RequireComponent(typeof(Health))]
public class AuraHolder : NetworkBehaviour
{
    [SerializeField] private AuraSettings _settings;
    [SerializeField] private ReactionTable _reactions;

    private readonly SyncVar<ElementType> _element = new();
    private readonly SyncVar<AuraStrength> _strength = new();
    private readonly SyncVar<NetworkObject> _owner = new();

    private Health _health;

    // Server side. Durations follow game time, like the rest of the simulation.
    private float _expiresAt;
    private float _reactionLockedUntil;
    private readonly Dictionary<(int sourceId, AttackSlot attack), float> _nextApplyTimes = new();

    public bool HasAura => _element.Value != ElementType.None;
    public ElementType Element => _element.Value;
    public AuraStrength Strength => _strength.Value;

    /// <summary>
    /// The player who applied or last refreshed it. Null without an aura, or if that player left.
    /// </summary>
    public NetworkObject Owner => _owner.Value;

    /// <summary>
    /// Raised on each client, host included, when a new value arrives from the server.
    /// </summary>
    public event Action<AuraHolder> Changed;

    private void Awake()
    {
        _health = GetComponent<Health>();

        _element.OnChange += OnElementChanged;
        _strength.OnChange += OnStrengthChanged;
        _owner.OnChange += OnOwnerChanged;

        if (_settings == null || _reactions == null)
            Debug.LogError("[AuraHolder] Aura settings or reaction table are not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled and scene objects come back with the state of their previous life.
        _nextApplyTimes.Clear();
        _reactionLockedUntil = 0f;
        Clear();

        _health.Died += OnDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _health.Died -= OnDied;
    }

    /// <summary>
    /// Server only. canDetonate is false for the basic attack and for skills that only mark.
    /// </summary>
    public AuraApplyResult TryApply(ElementType element, AuraStrength strength, NetworkObject source,
                                    AttackSlot attack, bool canDetonate)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[AuraHolder] Only the server can apply auras.", this);
            return AuraApplyResult.Ignored;
        }

        if (element == ElementType.None || _settings == null || _health.IsDead)
            return AuraApplyResult.Ignored;

        float now = Time.time;
        (int, AttackSlot) key = (source != null ? source.ObjectId : -1, attack);

        if (_nextApplyTimes.TryGetValue(key, out float nextApplyTime) && now < nextApplyTime)
            return AuraApplyResult.Ignored;

        if (HasAura && _element.Value != element)
            return TryReact(element, source, canDetonate, now) ? AuraApplyResult.Reacted : AuraApplyResult.Ignored;

        float duration = GetDuration(strength, source);
        AuraApplyResult result;

        if (!HasAura)
        {
            _element.Value = element;
            _strength.Value = strength;
            _expiresAt = now + duration;
            result = AuraApplyResult.Applied;
        }
        else
        {
            // Same element: a refresh never shortens it. Whichever lasts longer stays, with its strength.
            if (now + duration > _expiresAt)
            {
                _strength.Value = strength;
                _expiresAt = now + duration;
            }

            result = AuraApplyResult.Refreshed;
        }

        // Refreshing always makes it yours, so the player who keeps it alive gets the credit.
        _owner.Value = source;
        _nextApplyTimes[key] = now + _settings.ApplyCooldown;
        return result;
    }

    // The base from AuraSettings, with the upgrades of the player who applies it (Auras tenaces).
    // The host has every player's stats, and auras are only decided on the host.
    private float GetDuration(AuraStrength strength, NetworkObject source)
    {
        float duration = _settings.GetDuration(strength);

        if (source == null || !source.TryGetComponent(out PlayerStats stats))
            return duration;

        StatId stat = strength == AuraStrength.Strong ? StatId.StrongAuraDuration : StatId.WeakAuraDuration;
        return stats.Get(stat, duration);
    }

    // Another element is already here. Only a skill that can detonate consumes it, outside the
    // lockout and with a row for the pair. Otherwise it is left alone, as the basic attack does.
    private bool TryReact(ElementType detonatorElement, NetworkObject detonator, bool canDetonate, float now)
    {
        if (!canDetonate || now < _reactionLockedUntil || _reactions == null)
            return false;

        if (!_reactions.TryGet(_element.Value, detonatorElement, out ReactionDefinition reaction))
            return false;

        NetworkObject auraOwner = _owner.Value;
        ElementType auraElement = _element.Value;

        // The detonator leaves no aura of its own.
        Clear();
        _reactionLockedUntil = now + _reactions.LockoutDuration;

        // Sent before the effects: they may kill and despawn this enemy, and a despawned object can't send RPCs.
        ObserversReaction(auraElement, detonatorElement, transform.position, auraOwner, detonator);

        GameServices.Instance.ReactionService.Resolve(new ReactionInfo(this, reaction, transform.position, auraOwner, detonator));
        return true;
    }

    // Only the two elements travel; each client finds the row in its own copy of the table.
    // The host's client receives it too, like any other observer.
    [ObserversRpc]
    private void ObserversReaction(ElementType auraElement, ElementType detonatorElement, Vector2 position,
                                   NetworkObject auraOwner, NetworkObject detonator)
    {
        if (_reactions == null || !_reactions.TryGet(auraElement, detonatorElement, out ReactionDefinition reaction))
        {
            Debug.LogWarning($"[AuraHolder] This client has no reaction for {auraElement} detonated by {detonatorElement}.", this);
            return;
        }

        GameServices.Instance.CombatEventService.RaiseReaction(new ReactionInfo(this, reaction, position, auraOwner, detonator));
    }

    /// <summary>
    /// Server only, for the reaction test scene: no aura, no lockout and no cooldowns,
    /// as if the enemy had just appeared.
    /// </summary>
    public void ResetForTesting()
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[AuraHolder] Only the server can reset auras.", this);
            return;
        }

        _nextApplyTimes.Clear();
        _reactionLockedUntil = 0f;
        Clear();
    }

    private void Update()
    {
        if (IsServerInitialized && HasAura && Time.time >= _expiresAt)
            Clear();
    }

    // So a training dummy doesn't come back to life with the aura it died with.
    private void OnDied(Health health, NetworkObject attacker)
    {
        Clear();
    }

    private void Clear()
    {
        _element.Value = ElementType.None;
        _strength.Value = AuraStrength.Weak;
        _owner.Value = null;
    }

    // Presentation follows what each client receives, as in Health.
    private void OnElementChanged(ElementType previous, ElementType next, bool asServer)
    {
        if (!asServer)
            Changed?.Invoke(this);
    }

    private void OnStrengthChanged(AuraStrength previous, AuraStrength next, bool asServer)
    {
        if (!asServer)
            Changed?.Invoke(this);
    }

    private void OnOwnerChanged(NetworkObject previous, NetworkObject next, bool asServer)
    {
        if (!asServer)
            Changed?.Invoke(this);
    }
}
