using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private HealthSettings _settings;

    private readonly SyncVar<int> _current = new();

    private float _invulnerableUntil;

    public int Current => _current.Value;
    public int Max => _settings != null ? _settings.MaxHealth : 0;
    public bool IsDead => _current.Value <= 0;

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

        // Pooled objects come back with the timer of their previous life.
        _invulnerableUntil = 0f;
    }

    /// <summary>
    /// Server only. Returns false if the hit was ignored.
    /// </summary>
    public bool TryTakeDamage(int amount, NetworkObject attacker)
    {
        if (!IsServerInitialized)
        {
            Debug.LogWarning("[Health] Only the server can apply damage.", this);
            return false;
        }

        if (amount <= 0 || IsDead || Time.time < _invulnerableUntil)
            return false;

        _current.Value = Mathf.Max(0, _current.Value - amount);
        _invulnerableUntil = Time.time + _settings.InvulnerabilityDuration;

        if (IsDead)
            Died?.Invoke(this, attacker);

        return true;
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

    private void OnCurrentChanged(int previous, int next, bool asServer)
    {
        // Presentation follows what each client receives, so the host's
        // client hears it at the same point as everyone else.
        if (asServer)
            return;

        Changed?.Invoke(this);
    }
}
