using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// Tells this player's upgrades what happens during the match (OnEventBlock): a skill cast or a
// reaction they take part in. For each OnEventBlock of the upgrades they have that listens to it, it checks
// the block's internal cooldowns and runs its actions.
//   On the host: the real thing. Skill casts come from PlayerSkills once the host accepts them; reactions
//   from ReactionService, as soon as one resolves.
//   On the owner: its own skill cast only, to show timed effects at once (Tempo). Only the actions
//   that predict do anything; the host's answer replaces them.
// The cooldowns are counted in ticks of the host's clock, from the tick of the event, so the host and the
// owner agree on when a block is ready again.
// On the player prefab, next to PlayerUpgrades.
[RequireComponent(typeof(PlayerUpgrades))]
public class PlayerUpgradeEvents : NetworkBehaviour
{
    private PlayerUpgrades _upgrades;
    private PlayerStats _stats;
    private Health _health;
    private ReactionService _reactions;

    // Server side: when each block can run again for this player, and for each partner it gives to.
    private readonly Dictionary<OnEventBlock, uint> _readyTicks = new();
    private readonly Dictionary<(OnEventBlock block, int partnerId), uint> _partnerReadyTicks = new();

    // Owner side, for its predictions: the same, as the owner believes it.
    private readonly Dictionary<OnEventBlock, uint> _predictedReadyTicks = new();

    private void Awake()
    {
        _upgrades = GetComponent<PlayerUpgrades>();
        TryGetComponent(out _stats);
        TryGetComponent(out _health);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ResetForNewRun();

        _reactions = GameServices.Instance != null ? GameServices.Instance.ReactionService : null;
        if (_reactions != null)
            _reactions.Resolved += OnReactionResolved;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_reactions != null)
            _reactions.Resolved -= OnReactionResolved;

        _reactions = null;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _predictedReadyTicks.Clear();
    }

    /// <summary>
    /// Server only. Every block ready again, for a restarted match.
    /// </summary>
    public void ResetForNewRun()
    {
        _readyTicks.Clear();
        _partnerReadyTicks.Clear();
    }

    /// <summary>
    /// Server only. The host accepted a skill cast of this player, cast on that tick.
    /// </summary>
    public void RaiseSkillCast(uint tick)
    {
        if (IsServerInitialized)
            Dispatch(new EffectEventInfo(EffectEvent.SkillCast, tick), predict: false);
    }

    /// <summary>
    /// Owner only, on a client. It just cast a skill on that tick and told the host.
    /// </summary>
    public void PredictSkillCast(uint tick)
    {
        if (!IsServerInitialized && IsOwner)
            Dispatch(new EffectEventInfo(EffectEvent.SkillCast, tick), predict: true);
    }

    // A reaction anywhere: this player cares if the aura was theirs or they detonated it. A player who
    // left shows up as null, so a duo with someone gone has no partner.
    private void OnReactionResolved(ReactionInfo reaction)
    {
        NetworkObject self = NetworkObject;
        bool isAuraOwner = reaction.AuraOwner == self;
        bool isDetonator = reaction.Detonator == self;
        if (!isAuraOwner && !isDetonator)
            return;

        NetworkObject partner = null;
        if (reaction.IsDuo)
            partner = isDetonator ? reaction.AuraOwner : reaction.Detonator;

        Dispatch(new EffectEventInfo(EffectEvent.Reaction, TimeManager.Tick, isAuraOwner, isDetonator, reaction.IsDuo, partner),
                 predict: false);
    }

    private void Dispatch(EffectEventInfo info, bool predict)
    {
        UpgradeOffers offers = UpgradeOffers.Current;
        if (offers == null || offers.Pool == null)
            return;

        foreach (KeyValuePair<string, int> stack in _upgrades.Stacks)
        {
            if (stack.Value <= 0 || !offers.Pool.TryGet(stack.Key, out UpgradeDefinition upgrade))
                continue;

            foreach (EffectBlock block in upgrade.Effects)
            {
                if (block is OnEventBlock onEvent && onEvent.Accepts(info))
                    TryRun(onEvent, upgrade, info, offers, predict);
            }
        }
    }

    // Once per event, whatever the stacks: no event upgrade stacks.
    private void TryRun(OnEventBlock block, UpgradeDefinition upgrade, EffectEventInfo info, UpgradeOffers offers, bool predict)
    {
        Dictionary<OnEventBlock, uint> readyTicks = predict ? _predictedReadyTicks : _readyTicks;
        if (readyTicks.TryGetValue(block, out uint readyTick) && info.Tick < readyTick)
            return;

        // Asistencia: the same partner gets it at most every few seconds.
        (OnEventBlock, int) partnerKey = (block, info.Partner != null ? info.Partner.ObjectId : -1);
        if (!predict && block.RecipientCooldown > 0f)
        {
            if (info.Partner == null)
                return;

            if (_partnerReadyTicks.TryGetValue(partnerKey, out uint partnerReady) && info.Tick < partnerReady)
                return;

            _partnerReadyTicks[partnerKey] = info.Tick + TimeManager.TimeToTicks(block.RecipientCooldown);
        }

        if (block.Cooldown > 0f)
            readyTicks[block] = info.Tick + TimeManager.TimeToTicks(block.Cooldown);

        EffectContext context = new(_upgrades, _stats, _health, upgrade, predict ? null : offers.Settings, info.Tick, info.Partner);

        if (predict)
            block.Predict(context);
        else
            block.Run(context);
    }
}
