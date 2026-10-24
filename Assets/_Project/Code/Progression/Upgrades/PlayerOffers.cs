using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The conversation between the host and one player about their level-up offers. The host decides all of it:
//   1. Each level up of the group queues an offer. The first of the queue is drawn when it becomes the
//      first, with the player's state at that moment, and sent only to its owner (TargetRpc).
//   2. The owner answers with the offer's id and what they chose (ServerRpc). The host checks it is the
//      offer still pending and that the choice still holds, applies it and opens the next one.
// What the player has (upgrades, rerolls, banishes, what was seen) is PlayerUpgrades; this only asks
// and applies the answers onto it.
[RequireComponent(typeof(PlayerUpgrades))]
public class PlayerOffers : NetworkBehaviour
{
    // Offers waiting, the open one included, for the HUD's counter.
    private readonly SyncVar<int> _pendingOffers = new();

    // Whether the owner has the offer panel open, so the group knows they're distracted.
    private readonly SyncVar<bool> _choosing = new();

    // Server side: the queue and the open offer. _offerId 0 means none is open.
    private readonly Queue<int> _queuedLevels = new();
    private readonly List<OfferOption> _options = new();
    private int _offerId;
    private int _offerLevel;
    private int _lastOfferId;
    private bool _ownerReady;
    private UpgradeOffers _offers;
    private float _nextConfirmInvulnerability;

    // Owner side: what it last told the host, not the replicated value, which lags behind by the ping.
    private bool _sentChoosing;

    private PlayerUpgrades _upgrades;
    private PlayerSkillSlots _skills;

    public PlayerUpgrades Upgrades => _upgrades;

    /// <summary>
    /// Every client, host included. Offers this player has waiting, the open one included.
    /// </summary>
    public int PendingOffers => _pendingOffers.Value;

    /// <summary>
    /// Every client, host included. The owner has the offer panel open.
    /// </summary>
    public bool IsChoosing => _choosing.Value;

    /// <summary>
    /// Owner side. The offer open now, as it arrived from the host. Null when there is none.
    /// </summary>
    public ReceivedOffer CurrentOffer { get; private set; }

    /// <summary>
    /// Owner side. A new offer arrived, the open one changed (reroll, banish) or there's none left.
    /// </summary>
    public event Action OfferChanged;

    private void Awake()
    {
        _upgrades = GetComponent<PlayerUpgrades>();

        // Optional, so a test player without skills still gets upgrade offers.
        TryGetComponent(out _skills);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _queuedLevels.Clear();
        _options.Clear();
        _offerId = 0;
        _ownerReady = false;
        _nextConfirmInvulnerability = 0f;
        _choosing.Value = false;

        // Null in a scene without offers, like ReactionLab: the player just never gets any.
        _offers = UpgradeOffers.Current;
        if (_offers != null)
            _offers.Register(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_offers != null)
            _offers.Unregister(this);

        _offers = null;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Only now can a TargetRpc reach the owner for sure: until this point its player may not exist
        // on its side yet. Offers queued before (joining late, the first level up) wait for this.
        if (IsOwner)
            ServerOwnerReady();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        CurrentOffer = null;
        _sentChoosing = false;
    }

    /// <summary>
    /// Server only. Nothing taken and no offers waiting: how every player starts a match.
    /// </summary>
    public void ResetForNewRun(int banishes)
    {
        if (!IsServerInitialized)
            return;

        _upgrades.ResetForNewRun(banishes);

        bool hadOffer = _offerId != 0;
        _queuedLevels.Clear();
        _options.Clear();
        _offerId = 0;
        UpdatePending();

        if (hadOffer && _ownerReady)
            TargetCloseOffer(Owner);
    }

    /// <summary>
    /// Server only. The group reached this level: one more offer in the queue. If none was open, it's
    /// drawn and sent now.
    /// </summary>
    public void EnqueueOffer(int level)
    {
        if (!IsServerInitialized)
            return;

        _queuedLevels.Enqueue(level);
        UpdatePending();
        OpenNext();
    }

    /// <summary>
    /// Owner side. Answers the open offer. index is the option for Pick and Banish, ignored otherwise.
    /// The host's reply (the next offer, or none) arrives with the latency; until then the offer is
    /// marked answered so the panel doesn't send twice.
    /// </summary>
    public void Answer(OfferAnswer answer, int index)
    {
        if (!IsOwner || CurrentOffer == null || CurrentOffer.IsAnswered)
            return;

        CurrentOffer.IsAnswered = true;
        ServerAnswer(CurrentOffer.Id, answer, index);
    }

    /// <summary>
    /// Owner side. Tells the group whether the offer panel is open. Only sends when it changes.
    /// </summary>
    public void SetChoosing(bool choosing)
    {
        if (!IsOwner || _sentChoosing == choosing)
            return;

        _sentChoosing = choosing;
        ServerSetChoosing(choosing);
    }

    [ServerRpc]
    private void ServerSetChoosing(bool choosing)
    {
        _choosing.Value = choosing;
    }

    [ServerRpc]
    private void ServerOwnerReady()
    {
        _ownerReady = true;

        if (_offerId != 0)
            SendOffer();
        else
            OpenNext();
    }

    // Only the owner can call it (ServerRpc requires ownership), so nobody answers someone else's offer.
    [ServerRpc]
    private void ServerAnswer(int offerId, OfferAnswer answer, int index)
    {
        // An answer to an offer that was rerolled, banished from or closed meanwhile: the owner already
        // has the newer one on its way.
        if (_offerId == 0 || offerId != _offerId || _offers == null)
            return;

        bool accepted = answer switch
        {
            OfferAnswer.Pick => Pick(index),
            OfferAnswer.Reroll => Reroll(),
            OfferAnswer.Banish => Banish(index),
            OfferAnswer.Skip => Skip(),
            _ => false,
        };

        // Refused (no reroll left, an option that can't be banished...): the same offer goes back, so the
        // owner's panel stops waiting for a reply.
        if (!accepted)
            SendOffer();
    }

    private bool Skip()
    {
        _upgrades.EarnRerollThirds(_offers.Settings.SkipRerollThirds, _offers.Settings.MaxRerollThirds);
        GrantConfirmInvulnerability();
        TargetAnswered(Owner, OfferAnswer.Skip, null, _offerLevel);
        CloseOffer();
        return true;
    }

    private bool Pick(int index)
    {
        if (index < 0 || index >= _options.Count)
            return false;

        OfferOption option = _options[index];

        // Someone else may have just taken an upgrade only one player can have: that option changes
        // and the offer goes back to the owner, who chooses again.
        if (!_offers.IsStillOfferable(_upgrades, _offerLevel, option))
        {
            if (!_offers.Replace(_upgrades, _offerLevel, _options, index, "sustituida"))
                _options.RemoveAt(index);

            SendOffer();
            return true;
        }

        if (option.IsSkill)
            return PickSkill(option);

        UpgradeDefinition upgrade = option.Upgrade;

        // The fallback isn't counted as taken: it can come again whenever nothing else is left.
        _upgrades.Acquire(upgrade, upgrade != _offers.Pool.Fallback, _offers.Settings);

        GrantConfirmInvulnerability();
        TargetAnswered(Owner, OfferAnswer.Pick, upgrade.Id, _offerLevel);
        CloseOffer();
        return true;
    }

    // A new skill goes into the next free slot; a level goes onto the skill. IsStillOfferable has just
    // checked that it fits, so a refusal here is a bug, and the offer stays open.
    private bool PickSkill(OfferOption option)
    {
        bool done = _skills != null &&
                    (option.SkillLevel <= 1 ? _skills.TryAddSkill(option.Skill) : _skills.TryLevelUp(option.Skill.Id));
        if (!done)
        {
            Debug.LogWarning($"[PlayerOffers] J{_upgrades.Slot + 1} couldn't take {option.Key}.", this);
            return false;
        }

        GrantConfirmInvulnerability();
        TargetAnswered(Owner, OfferAnswer.Pick, option.Key, _offerLevel);
        CloseOffer();
        return true;
    }

    // The same level and guarantees, three new options; an evolution stays. What was on screen weighs
    // half in the new draw.
    private bool Reroll()
    {
        if (!_upgrades.TrySpendReroll())
            return false;

        _upgrades.SetSeen(CurrentIds());
        _offers.Generate(_upgrades, _offerLevel, _options, "reroll");
        TargetAnswered(Owner, OfferAnswer.Reroll, null, _offerLevel);
        SendOffer();
        return true;
    }

    // Out of the player's offers for the rest of the match, and another option takes its place.
    private bool Banish(int index)
    {
        if (index < 0 || index >= _options.Count || !_options[index].CanBanish)
            return false;

        string id = _options[index].Key;
        if (!_upgrades.TryBanish(id))
            return false;

        if (!_offers.Replace(_upgrades, _offerLevel, _options, index, "descarte"))
            _options.RemoveAt(index);

        TargetAnswered(Owner, OfferAnswer.Banish, id, _offerLevel);
        SendOffer();
        return true;
    }

    // A moment without hits for having looked at the panel, with a cooldown: picking three queued offers
    // in a row gives it once.
    private void GrantConfirmInvulnerability()
    {
        OfferSettings settings = _offers.Settings;
        if (settings.ConfirmInvulnerability <= 0f || Time.time < _nextConfirmInvulnerability)
            return;

        if (!TryGetComponent(out Health health) || health.IsDead)
            return;

        health.AddInvulnerability(settings.ConfirmInvulnerability);
        _nextConfirmInvulnerability = Time.time + settings.ConfirmInvulnerabilityCooldown;
    }

    private void OpenNext()
    {
        if (!_ownerReady || _offerId != 0 || _queuedLevels.Count == 0 || _offers == null)
            return;

        _offerLevel = _queuedLevels.Dequeue();
        _options.Clear();
        _offers.Generate(_upgrades, _offerLevel, _options, "nueva");
        SendOffer();
    }

    private void CloseOffer()
    {
        _upgrades.SetSeen(CurrentIds());
        _options.Clear();
        _offerId = 0;
        UpdatePending();

        if (_queuedLevels.Count > 0)
            OpenNext();
        else
            TargetCloseOffer(Owner);
    }

    // Every change to the open offer gets a new id, so a late answer to the old one is ignored.
    private void SendOffer()
    {
        _offerId = ++_lastOfferId;
        UpdatePending();

        string[] ids = CurrentIds();
        bool[] banishable = new bool[_options.Count];
        for (int i = 0; i < _options.Count; i++)
            banishable[i] = _options[i].CanBanish;

        TargetOffer(Owner, _offerId, _offerLevel, ids, banishable);
    }

    private string[] CurrentIds()
    {
        string[] ids = new string[_options.Count];
        for (int i = 0; i < _options.Count; i++)
            ids[i] = _options[i].Key;

        return ids;
    }

    private void UpdatePending()
    {
        int pending = _queuedLevels.Count + (_offerId != 0 ? 1 : 0);
        if (_pendingOffers.Value != pending)
            _pendingOffers.Value = pending;
    }

    [TargetRpc]
    private void TargetOffer(NetworkConnection target, int offerId, int level, string[] ids, bool[] banishable)
    {
        UpgradeOffers offers = UpgradeOffers.Current;
        if (offers == null || offers.Pool == null)
            return;

        OfferOption?[] options = new OfferOption?[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            // Host and clients run the same build, so a missing id is a bug worth seeing.
            if (!TryResolve(offers, ids[i], out OfferOption option))
                Debug.LogError($"[PlayerOffers] The host offered «{ids[i]}», which this build doesn't have.", this);
            else
                options[i] = option;
        }

        CurrentOffer = new ReceivedOffer(offerId, level, options, banishable);
        OfferChanged?.Invoke();
    }

    // An upgrade's id, or a skill's key: the panel only needs what it shows, so the place is left open.
    private bool TryResolve(UpgradeOffers offers, string key, out OfferOption option)
    {
        if (OfferOption.TryParseSkillKey(key, out string skillId, out int skillLevel))
        {
            SkillDefinition skill = _skills != null && _skills.Library != null ? _skills.Library.Get(skillId) : null;
            option = new OfferOption(skill, skillLevel, OfferPlace.Open);
            return skill != null;
        }

        bool found = offers.Pool.TryGet(key, out UpgradeDefinition upgrade);
        option = new OfferOption(upgrade, OfferPlace.Open);
        return found;
    }

    [TargetRpc]
    private void TargetCloseOffer(NetworkConnection target)
    {
        CurrentOffer = null;
        OfferChanged?.Invoke();
    }

    // The host accepted an answer: the launcher hears it from the player who made it.
    [TargetRpc]
    private void TargetAnswered(NetworkConnection target, OfferAnswer answer, string upgradeId, int level)
    {
        int matchTime = MatchClock.Current != null ? MatchClock.Current.ElapsedSeconds : 0;

        switch (answer)
        {
            // A skill taken from an offer is its own event: ability and level.
            case OfferAnswer.Pick when OfferOption.TryParseSkillKey(upgradeId, out string skillId, out int skillLevel):
                LauncherEvents.SkillGained(skillId, skillLevel, level, matchTime);
                break;

            case OfferAnswer.Pick:
                UpgradeDefinition upgrade = null;
                if (UpgradeOffers.Current != null && UpgradeOffers.Current.Pool != null)
                    UpgradeOffers.Current.Pool.TryGet(upgradeId, out upgrade);

                string rarity = LauncherEvents.RarityId(upgrade != null ? upgrade.Rarity : UpgradeRarity.Common);
                LauncherEvents.UpgradePicked(upgradeId, rarity, level, matchTime);
                break;

            case OfferAnswer.Reroll:
                LauncherEvents.OfferAction("reroll", null, level, matchTime);
                break;

            case OfferAnswer.Banish when OfferOption.TryParseSkillKey(upgradeId, out string banishedSkill, out _):
                LauncherEvents.OfferAction("banish", null, level, matchTime, banishedSkill);
                break;

            case OfferAnswer.Banish:
                LauncherEvents.OfferAction("banish", upgradeId, level, matchTime);
                break;

            case OfferAnswer.Skip:
                LauncherEvents.OfferAction("skip", null, level, matchTime);
                break;
        }
    }
}
