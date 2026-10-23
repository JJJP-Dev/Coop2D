using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;

// The stats of one player (#157): their bonuses from upgrades and the final value of each stat.
// The host decides the bonuses and is the truth. Only the owner receives them too, because the owner
// simulates its own movement and shots (#156) and must use the same numbers the host checks them with.
// Host and owner work out every stat with the same function (PlayerStatSettings.Calculate).
// The other players don't need them: what they see of this player (health, position) already travels.
// The base values stay where they were (movement, basic attack, health and orb settings): each system
// asks for its stat with its own base, Get(StatId.BasicRange, settings.Range).
// Timed bonuses (Tempo, Viento a favor) last between two ticks of the host's clock, so the host and the
// owner stop counting them on the same tick. For its own skill cast the owner shows them at once, before
// the host's copy arrives (prediction, #157).
// On the player prefab.
public class PlayerStats : NetworkBehaviour
{
    private static readonly int StatCount = Enum.GetValues(typeof(StatId)).Length;

    [SerializeField] private PlayerStatSettings _settings;

    // Every bonus the player has. Owner only: nobody else reads them.
    private readonly SyncList<StatBonus> _bonuses = new(new SyncTypeSettings(ReadPermission.OwnerOnly));

    // The bonuses that only last a while (#157). Owner only too, and sent on the next tick instead of
    // every 0.1 s: they last 2 s, and every tick they arrive late is a tick the owner doesn't feel them.
    private readonly SyncList<TimedStatBonus> _timedBonuses =
        new(new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly, 0f, Channel.Reliable));

    // Owner side: the timed bonuses it showed ahead of the host. Each one goes when the host's copy
    // arrives, when it ends, or a second after it started if the host never sends it (it didn't accept
    // the cast, or the upgrade's internal cooldown wasn't over for the host).
    private readonly List<TimedStatBonus> _predictedBonuses = new();
    private const double PredictionWindow = 1.0;

    // The sums of the bonuses, per stat. Worked out again whenever the list changes, on the host when it
    // changes it and on the owner when the change arrives.
    private float[] _flat;
    private float[] _percent;

    // Server only: the bonuses that depend on the target (#161). They don't travel, because only the
    // host deals damage. Few, so Get goes through them every time instead of keeping sums.
    private readonly List<ConditionalBonus> _conditionalBonuses = new();

    // The next tick a timed bonus starts or ends, when the sums must be worked out again.
    // uint.MaxValue: none pending.
    private uint _nextTimedChange = uint.MaxValue;

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
        _timedBonuses.OnChange += OnTimedBonusesChanged;

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
        _timedBonuses.Clear();
        _conditionalBonuses.Clear();
        Recalculate();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _predictedBonuses.Clear();
    }

    // Timed bonuses end by themselves: when one does, the sums change.
    private void Update()
    {
        if (_nextTimedChange == uint.MaxValue || (!IsServerInitialized && !IsOwner))
            return;

        if (TimeManager == null || TimeManager.Tick < _nextTimedChange)
            return;

        if (IsServerInitialized)
            RemoveEndedTimedBonuses();

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
    /// Server only. The final value of this stat for a hit on a target that meets these conditions:
    /// the normal bonuses plus the conditional ones whose conditions are all met.
    /// </summary>
    public float Get(StatId stat, float baseValue, TargetConditions met)
    {
        int index = (int)stat;
        float flat = _flat[index];
        float percent = _percent[index];

        foreach (ConditionalBonus conditional in _conditionalBonuses)
        {
            if (conditional.Bonus.Stat != stat || (conditional.Conditions & met) != conditional.Conditions)
                continue;

            if (conditional.Bonus.Unit == StatUnit.Percent)
                percent += conditional.Bonus.Value;
            else
                flat += conditional.Bonus.Value;
        }

        if (_settings == null)
            return (baseValue + flat) * (1f + percent / 100f);

        return _settings.Calculate(stat, baseValue, flat, percent);
    }

    /// <summary>
    /// Server only. One more bonus that only counts on some hits, for the rest of the match.
    /// </summary>
    public void AddConditionalBonus(TargetConditions conditions, StatBonus bonus)
    {
        if (!IsServerInitialized)
            return;

        _conditionalBonuses.Add(new ConditionalBonus(conditions, bonus));
        Changed?.Invoke();
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
    /// Server only. A bonus for duration seconds from startTick, in the host's clock: the tick of the event
    /// that gave it, so it ends on the same tick the owner predicted. 0 means now.
    /// </summary>
    public void AddTimedBonus(StatBonus bonus, uint startTick, float duration)
    {
        if (!IsServerInitialized || duration <= 0f)
            return;

        if (startTick == 0)
            startTick = TimeManager.Tick;

        _timedBonuses.Add(new TimedStatBonus(bonus, startTick, startTick + TimeManager.TimeToTicks(duration)));
        Recalculate();
    }

    /// <summary>
    /// Owner only, on a client: the same bonus the host is about to add, shown at once (#157).
    /// </summary>
    public void PredictTimedBonus(StatBonus bonus, uint startTick, float duration)
    {
        if (IsServerInitialized || !IsOwner || duration <= 0f)
            return;

        _predictedBonuses.Add(new TimedStatBonus(bonus, startTick, startTick + TimeManager.TimeToTicks(duration)));
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
        _timedBonuses.Clear();
        _conditionalBonuses.Clear();
        Recalculate();
    }

    // The host doesn't wait for the change to come back: what it just added already counts.
    private void Recalculate()
    {
        Array.Clear(_flat, 0, _flat.Length);
        Array.Clear(_percent, 0, _percent.Length);

        foreach (StatBonus bonus in _bonuses)
            AddToSums(bonus);

        AddTimedToSums();

        // Maximum health is the one stat everyone sees: Health sends it to every client.
        if (IsServerInitialized && _health != null)
            _health.SetMaxHealth(Mathf.RoundToInt(Get(StatId.MaxHealth, _health.BaseMax)));

        Changed?.Invoke();
    }

    private void AddToSums(StatBonus bonus)
    {
        int index = (int)bonus.Stat;
        if (index < 0 || index >= StatCount)
            return;

        if (bonus.Unit == StatUnit.Percent)
            _percent[index] += bonus.Value;
        else
            _flat[index] += bonus.Value;
    }

    // The timed bonuses active on this tick, and the next tick one starts or ends. The owner adds its
    // predicted ones that the host hasn't confirmed yet.
    private void AddTimedToSums()
    {
        _nextTimedChange = uint.MaxValue;

        if (TimeManager == null)
            return;

        uint tick = TimeManager.Tick;

        foreach (TimedStatBonus timed in _timedBonuses)
            CountTimed(timed, tick);

        if (IsServerInitialized || _predictedBonuses.Count == 0)
            return;

        uint window = TimeManager.TimeToTicks(PredictionWindow);
        for (int i = _predictedBonuses.Count - 1; i >= 0; i--)
        {
            TimedStatBonus predicted = _predictedBonuses[i];
            bool unconfirmed = tick >= predicted.StartTick + window;

            if (tick >= predicted.EndTick || unconfirmed || IsConfirmed(predicted))
            {
                _predictedBonuses.RemoveAt(i);
                continue;
            }

            CountTimed(predicted, tick);
            _nextTimedChange = Math.Min(_nextTimedChange, predicted.StartTick + window);
        }
    }

    private void CountTimed(TimedStatBonus timed, uint tick)
    {
        if (timed.IsActive(tick))
        {
            AddToSums(timed.Bonus);
            _nextTimedChange = Math.Min(_nextTimedChange, timed.EndTick);
        }
        else if (tick < timed.StartTick)
        {
            _nextTimedChange = Math.Min(_nextTimedChange, timed.StartTick);
        }
    }

    // The host's copy of a predicted bonus: the same upgrade and change, and still running when the
    // predicted one started. An older one from the same upgrade has ended by then (its cooldown is
    // longer than its duration), so it never counts as the copy.
    private bool IsConfirmed(TimedStatBonus predicted)
    {
        foreach (TimedStatBonus timed in _timedBonuses)
        {
            if (timed.Bonus.Stat == predicted.Bonus.Stat && timed.Bonus.Unit == predicted.Bonus.Unit &&
                timed.Bonus.Source == predicted.Bonus.Source && timed.EndTick > predicted.StartTick)
                return true;
        }

        return false;
    }

    // Server only. So the list doesn't keep growing during the match.
    private void RemoveEndedTimedBonuses()
    {
        uint tick = TimeManager.Tick;
        for (int i = _timedBonuses.Count - 1; i >= 0; i--)
        {
            if (tick >= _timedBonuses[i].EndTick)
                _timedBonuses.RemoveAt(i);
        }
    }

    // On the owner's client, once per batch of changes. The host already worked them out when it made them.
    private void OnBonusesChanged(SyncListOperation operation, int index, StatBonus previous, StatBonus next, bool asServer)
    {
        if (!asServer && !IsServerInitialized && operation == SyncListOperation.Complete)
            Recalculate();
    }

    private void OnTimedBonusesChanged(SyncListOperation operation, int index, TimedStatBonus previous, TimedStatBonus next, bool asServer)
    {
        if (!asServer && !IsServerInitialized && operation == SyncListOperation.Complete)
            Recalculate();
    }
}
