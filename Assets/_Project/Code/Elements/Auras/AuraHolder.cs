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

    private readonly SyncVar<ElementType> _element = new();
    private readonly SyncVar<AuraStrength> _strength = new();
    private readonly SyncVar<NetworkObject> _owner = new();

    private Health _health;

    // Server side. Durations follow game time, like the rest of the simulation.
    private float _expiresAt;
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

        if (_settings == null)
            Debug.LogError("[AuraHolder] Aura settings are not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled and scene objects come back with the state of their previous life.
        _nextApplyTimes.Clear();
        Clear();

        _health.Died += OnDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _health.Died -= OnDied;
    }

    /// <summary>
    /// Server only. Returns true if the aura was applied or refreshed.
    /// </summary>
    public bool TryApply(ElementType element, AuraStrength strength, NetworkObject source, AttackSlot attack)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[AuraHolder] Only the server can apply auras.", this);
            return false;
        }

        if (element == ElementType.None || _settings == null || _health.IsDead)
            return false;

        float now = Time.time;
        (int, AttackSlot) key = (source != null ? source.ObjectId : -1, attack);

        if (_nextApplyTimes.TryGetValue(key, out float nextApplyTime) && now < nextApplyTime)
            return false;

        // Another element: the basic attack and skills that can't detonate leave it alone.
        // Detonating it is the reaction system's job (#64).
        if (HasAura && _element.Value != element)
            return false;

        float duration = _settings.GetDuration(strength);

        if (!HasAura)
        {
            _element.Value = element;
            _strength.Value = strength;
            _expiresAt = now + duration;
        }
        // Same element: a refresh never shortens it. Whichever lasts longer stays, with its strength.
        else if (now + duration > _expiresAt)
        {
            _strength.Value = strength;
            _expiresAt = now + duration;
        }

        // Refreshing always makes it yours, so the player who keeps it alive gets the credit.
        _owner.Value = source;
        _nextApplyTimes[key] = now + _settings.ApplyCooldown;
        return true;
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
