using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The stats of one player (#157): their bonuses from upgrades and the final value of each stat.
// The host decides the bonuses and is the truth. Only the owner receives them too, because the owner
// simulates its own movement and shots (#156) and must use the same numbers the host checks them with.
// Host and owner work out every stat with the same function (PlayerStatSettings.Calculate).
// The other players don't need them: what they see of this player (health, position) already travels.
// The base values stay where they were (movement, basic attack, health and orb settings): each system
// asks for its stat with its own base, Get(StatId.BasicRange, settings.Range).
// On the player prefab.
public class PlayerStats : NetworkBehaviour
{
    private static readonly int StatCount = Enum.GetValues(typeof(StatId)).Length;

    [SerializeField] private PlayerStatSettings _settings;

    // Every bonus the player has. Owner only: nobody else reads them.
    private readonly SyncList<StatBonus> _bonuses = new(new SyncTypeSettings(ReadPermission.OwnerOnly));

    // The sums of the bonuses, per stat. Worked out again whenever the list changes, on the host when it
    // changes it and on the owner when the change arrives.
    private float[] _flat;
    private float[] _percent;

    private Health _health;

    /// <summary>
    /// On the host and on the owner: the bonuses changed, so the stats may have.
    /// </summary>
    public event Action Changed;

    private void Awake()
    {
        _flat = new float[StatCount];
        _percent = new float[StatCount];

        _bonuses.OnChange += OnBonusesChanged;

        // Optional, so the stats work on a test object without health.
        TryGetComponent(out _health);

        if (_settings == null)
            Debug.LogError("[PlayerStats] Stat settings are not assigned. Stats will have no caps.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled players come back with the bonuses of their previous life.
        _bonuses.Clear();
        Recalculate();
    }

    /// <summary>
    /// On the host and on the owner: the final value of this stat, from its base. Elsewhere it's the base,
    /// because the bonuses don't travel there.
    /// </summary>
    public float Get(StatId stat, float baseValue)
    {
        int index = (int)stat;
        if (_settings == null)
            return (baseValue + _flat[index]) * (1f + _percent[index] / 100f);

        return _settings.Calculate(stat, baseValue, _flat[index], _percent[index]);
    }

    /// <summary>
    /// Server only. One more bonus, for the rest of the match.
    /// </summary>
    public void AddBonus(StatBonus bonus)
    {
        if (!IsServerInitialized)
            return;

        _bonuses.Add(bonus);
        Recalculate();
    }

    /// <summary>
    /// Server only. No bonuses at all, for a restarted match (#52).
    /// </summary>
    public void ClearBonuses()
    {
        if (!IsServerInitialized)
            return;

        _bonuses.Clear();
        Recalculate();
    }

    // The host doesn't wait for the change to come back: what it just added already counts.
    private void Recalculate()
    {
        Array.Clear(_flat, 0, _flat.Length);
        Array.Clear(_percent, 0, _percent.Length);

        foreach (StatBonus bonus in _bonuses)
        {
            int index = (int)bonus.Stat;
            if (index < 0 || index >= StatCount)
                continue;

            if (bonus.Unit == StatUnit.Percent)
                _percent[index] += bonus.Value;
            else
                _flat[index] += bonus.Value;
        }

        // Maximum health is the one stat everyone sees: Health sends it to every client.
        if (IsServerInitialized && _health != null)
            _health.SetMaxHealth(Mathf.RoundToInt(Get(StatId.MaxHealth, _health.BaseMax)));

        Changed?.Invoke();
    }

    // On the owner's client, once per batch of changes. The host already worked them out when it made them.
    private void OnBonusesChanged(SyncListOperation operation, int index, StatBonus previous, StatBonus next, bool asServer)
    {
        if (!asServer && !IsServerInitialized && operation == SyncListOperation.Complete)
            Recalculate();
    }
}
