using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private HealthSettings _settings;

    private readonly SyncVar<int> _current = new();
    private readonly SyncVar<bool> _invulnerable = new();

    private float _invulnerableUntil;

    /// <summary>
    /// Server only. Time.time of the last hit it took, rejected hits not counted. Revives use it:
    /// a teammate who was just hit stops helping for a moment (#79).
    /// </summary>
    public float LastDamagedAt { get; private set; } = float.NegativeInfinity;

    public int Current => _current.Value;
    public int Max => _settings != null ? _settings.MaxHealth : 0;
    public bool IsDead => _current.Value <= 0;

    /// <summary>
    /// While true, only Kill can hurt it. For now only the cheats panel turns it on.
    /// </summary>
    public bool IsInvulnerable => _invulnerable.Value;

    /// <summary>
    /// Raised on each client, host included, when a new value arrives from the server.
    /// </summary>
    public event Action<Health> Changed;

    /// <summary>
    /// Server only. The second argument is the attacker, or null.
    /// </summary>
    public event Action<Health, NetworkObject> Died;

    private void Awake()
    {
        _current.OnChange += OnCurrentChanged;
        _invulnerable.OnChange += OnInvulnerableChanged;

        if (_settings == null)
        {
            Debug.LogError("[Health] Health settings are not assigned.", this);
            return;
        }

        // Set before the network starts, so it is the starting value on every side.
        _current.Value = _settings.MaxHealth;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled objects come back with the invulnerability and the last hit of their previous life.
        _invulnerableUntil = 0f;
        LastDamagedAt = float.NegativeInfinity;
        _invulnerable.Value = false;
    }

    /// <summary>
    /// Server only. Returns false if the hit was ignored.
    /// Accepted hits reach every client through CombatEventService.
    /// </summary>
    public bool TryTakeDamage(int amount, NetworkObject attacker, DamageKind kind = DamageKind.Other,
                              ElementType element = ElementType.None)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can apply damage.", this);
            return false;
        }

        if (amount <= 0 || IsDead || _invulnerable.Value || Time.time < _invulnerableUntil)
            return false;

        ApplyDamage(amount, attacker, kind, element);
        return true;
    }

    /// <summary>
    /// Server only. Takes all its health at once, ignoring invulnerability,
    /// with the same hit and death as any other.
    /// </summary>
    public void Kill(NetworkObject attacker = null)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can kill.", this);
            return;
        }

        if (!IsDead)
            ApplyDamage(_current.Value, attacker, DamageKind.Other, ElementType.None);
    }

    /// <summary>
    /// Server only.
    /// </summary>
    public void RestoreFull()
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can restore health.", this);
            return;
        }

        _current.Value = Max;
        _invulnerableUntil = 0f;
    }

    /// <summary>
    /// Server only, before Spawn. It appears at 0 health: a player who joins a match that is already
    /// over arrives downed, like everyone else (#52). Set before spawning, so the 0 travels with the spawn
    /// and nobody sees it fall.
    /// </summary>
    public void SpawnDowned()
    {
        if (IsSpawned)
        {
            Debug.LogWarning("[Health] SpawnDowned only works before the object is spawned.", this);
            return;
        }

        _current.Value = 0;
    }

    /// <summary>
    /// Server only. Gets a downed player up (#51) with this much health, and ignores hits for a few
    /// seconds so the enemies around it can't drop it again at once. Does nothing unless it's at 0.
    /// </summary>
    public void Revive(int health, float invulnerableSeconds)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can revive.", this);
            return;
        }

        if (!IsDead)
            return;

        _current.Value = Mathf.Clamp(health, 1, Max);
        _invulnerableUntil = Time.time + invulnerableSeconds;
    }

    /// <summary>
    /// Server only. Ignores hits for a few seconds from now, or longer if it already did. Choosing an
    /// upgrade uses it (#77): the player looked away from the fight to pick.
    /// </summary>
    public void AddInvulnerability(float seconds)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can change invulnerability.", this);
            return;
        }

        _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
    }

    /// <summary>
    /// Server only.
    /// </summary>
    public void SetInvulnerable(bool invulnerable)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can change invulnerability.", this);
            return;
        }

        _invulnerable.Value = invulnerable;
    }

    private void ApplyDamage(int amount, NetworkObject attacker, DamageKind kind, ElementType element)
    {
        _current.Value = Mathf.Max(0, _current.Value - amount);
        _invulnerableUntil = Time.time + _settings.InvulnerabilityDuration;
        LastDamagedAt = Time.time;

        bool died = IsDead;

        // Sent before Died: a listener may despawn this object, and a despawned object can't send RPCs.
        ObserversHit(amount, attacker, transform.position, died, kind, element);

        if (died)
            Died?.Invoke(this, attacker);
    }

    // The host's client receives it too, like any other observer.
    [ObserversRpc]
    private void ObserversHit(int amount, NetworkObject attacker, Vector2 position, bool died,
                              DamageKind kind, ElementType element)
    {
        CombatEventService events = GameServices.Instance.CombatEventService;
        HitInfo hit = new(this, attacker, position, amount, kind, element);

        events.RaiseHit(hit);

        if (died)
            events.RaiseDeath(hit);
    }

    private void OnCurrentChanged(int previous, int next, bool asServer)
    {
        // Presentation follows what each client receives, so the host's
        // client hears it at the same point as everyone else.
        if (asServer)
            return;

        Changed?.Invoke(this);
    }

    private void OnInvulnerableChanged(bool previous, bool next, bool asServer)
    {
        if (!asServer)
            Changed?.Invoke(this);
    }
}
