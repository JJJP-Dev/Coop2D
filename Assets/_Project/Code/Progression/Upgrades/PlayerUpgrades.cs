using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The upgrades of one player in this match (#78), their queue of offers and what they have to change
// them with: rerolls, counted in thirds, and banishes. The host decides all of it (catalog C.7):
//   1. Each level up of the group queues an offer. The first of the queue is drawn when it becomes the
//      first, with the player's state at that moment, and sent only to its owner (TargetRpc).
//   2. The owner answers with the offer's id and what they chose (ServerRpc). The host checks it is the
//      offer still pending and that the choice still holds, applies it and opens the next one.
// The upgrades each player has travel to every client, because the group's HUD will show them.
// On the player prefab, next to PlayerSkills.
[RequireComponent(typeof(PlayerIdentity))]
public class PlayerUpgrades : NetworkBehaviour, IUpgradeHolder
{
    // Upgrade id and how many times this player took it. Ids and not indexes: they read the same in
    // a log, and a new upgrade in the pool can't shift the others.
    private readonly SyncDictionary<string, int> _stacks = new();

    // Thirds of a reroll the player can spend now, and banishes left. Everyone receives them; only the
    // owner's panel shows them for now.
    private readonly SyncVar<int> _rerollThirds = new();
    private readonly SyncVar<int> _banishesLeft = new();

    // Offers waiting, the open one included, for the HUD's counter (#77).
    private readonly SyncVar<int> _pendingOffers = new();

    // Whether the owner has the offer panel open, so the group knows they're distracted (#77).
    private readonly SyncVar<bool> _choosing = new();

    // Server side. Earned counts every third ever received in this match, spent ones included, so the
    // limit of 2 rerolls per match holds whatever the order of earning and spending (catalog C.5).
    private int _earnedThirds;
    private int _spentThirds;
    private readonly HashSet<string> _banished = new();
    private readonly HashSet<string> _seen = new();
    private int _offersWithoutDuo;

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

    private PlayerIdentity _identity;
    private PlayerElement _element;
    private PlayerCharacter _character;
    private PlayerSkills _skills;
    private PlayerStats _stats;
    private Health _health;
    private PlayerUpgradeEvents _events;
    private bool _hasProjectileBasic;

    // Requirement kinds this build doesn't know, each warned about once.
    private static readonly HashSet<string> WarnedKinds = new();

    public int Slot => _identity.Slot;

    /// <summary>
    /// Every client, host included. Rerolls the player can use now, whole.
    /// </summary>
    public int Rerolls => _rerollThirds.Value / 3;

    /// <summary>
    /// Every client, host included. The third of a reroll on its way, from 0 to 2.
    /// </summary>
    public int RerollThirdsOver => _rerollThirds.Value % 3;

    public int BanishesLeft => _banishesLeft.Value;

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

    /// <summary>
    /// rarity_luck, which moves weight from common to rare and epic. 0 until player stats exist (#157).
    /// </summary>
    public int Luck => 0;

    /// <summary>
    /// Server only. Offers in a row that could have had a duo and didn't (catalog C.4).
    /// </summary>
    public int OffersWithoutDuo => _offersWithoutDuo;

    /// <summary>
    /// Every client, host included. The upgrades this player has, by id, and how many times.
    /// </summary>
    public IReadOnlyDictionary<string, int> Stacks => _stacks;

    /// <summary>
    /// Client side, the host's client included. This player's upgrades changed.
    /// </summary>
    public event Action UpgradesChanged;

    private void Awake()
    {
        _identity = GetComponent<PlayerIdentity>();
        _stacks.OnChange += OnStacksChanged;

        // Optional, so a test player without them still works: it just meets fewer requirements.
        TryGetComponent(out _element);
        TryGetComponent(out _character);
        TryGetComponent(out _skills);
        TryGetComponent(out _stats);
        TryGetComponent(out _health);
        TryGetComponent(out _events);

        // Every basic attack is a projectile until the melee basic arrives (#96).
        _hasProjectileBasic = GetComponent<PlayerAutoAttack>() != null;
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

    public int GetStacks(string id)
    {
        return !string.IsNullOrEmpty(id) && _stacks.TryGetValue(id, out int stacks) ? stacks : 0;
    }

    /// <summary>
    /// Server only. Nothing taken, nothing banished, no rerolls: how every player starts a match.
    /// </summary>
    public void ResetForNewRun(int banishes)
    {
        if (!IsServerInitialized)
            return;

        _stacks.Clear();
        _banished.Clear();

        // What the upgrades gave goes with them (#157), and their internal cooldowns too (#161).
        if (_stats != null)
            _stats.ClearBonuses();

        if (_events != null)
            _events.ResetForNewRun();

        // Back to the starting skill alone: the others come from the offers (#162).
        if (_skills != null)
            _skills.ResetSkills();

        _seen.Clear();
        _offersWithoutDuo = 0;
        _earnedThirds = 0;
        _spentThirds = 0;
        _rerollThirds.Value = 0;
        _banishesLeft.Value = Mathf.Max(0, banishes);

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

    /// <summary>
    /// Server only. One more of this upgrade, up to its maximum.
    /// </summary>
    public void AddStack(UpgradeDefinition upgrade)
    {
        if (!IsServerInitialized || upgrade == null || string.IsNullOrEmpty(upgrade.Id))
            return;

        int stacks = GetStacks(upgrade.Id);
        if (stacks >= upgrade.MaxStacks)
        {
            Debug.LogWarning($"[PlayerUpgrades] J{Slot + 1} already has {upgrade.Id} {stacks} times, its maximum.", this);
            return;
        }

        _stacks[upgrade.Id] = stacks + 1;
    }

    /// <summary>
    /// Server only. Thirds of a reroll, from any source. Past the match's limit they're lost.
    /// </summary>
    public void EarnRerollThirds(int thirds, int maxThirds)
    {
        if (!IsServerInitialized || thirds <= 0)
            return;

        _earnedThirds = Mathf.Min(maxThirds, _earnedThirds + thirds);
        _rerollThirds.Value = _earnedThirds - _spentThirds;
    }

    /// <summary>
    /// Server only. Spends a whole reroll if there is one.
    /// </summary>
    public bool TrySpendReroll()
    {
        if (!IsServerInitialized || _rerollThirds.Value < 3)
            return false;

        _spentThirds += 3;
        _rerollThirds.Value = _earnedThirds - _spentThirds;
        return true;
    }

    /// <summary>
    /// Server only. Takes this upgrade out of the player's offers for the rest of the match.
    /// </summary>
    public bool TryBanish(string id)
    {
        if (!IsServerInitialized || _banishesLeft.Value <= 0 || string.IsNullOrEmpty(id) || !_banished.Add(id))
            return false;

        _banishesLeft.Value--;
        return true;
    }

    /// <summary>
    /// Server only.
    /// </summary>
    public bool IsBanished(string id)
    {
        return _banished.Contains(id);
    }

    /// <summary>
    /// Server only. Whether it was in the last offer this player saw: it weighs half in the next (C.1).
    /// </summary>
    public bool WasSeen(string id)
    {
        return _seen.Contains(id);
    }

    /// <summary>
    /// Server only. What the player saw in the offer that just ended, replacing the one before.
    /// </summary>
    public void SetSeen(IEnumerable<string> ids)
    {
        _seen.Clear();

        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
                _seen.Add(id);
        }
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
        EarnRerollThirds(_offers.Settings.SkipRerollThirds, _offers.Settings.MaxRerollThirds);
        GrantConfirmInvulnerability();
        TargetAnswered(Owner, OfferAnswer.Skip, null, _offerLevel);
        CloseOffer();
        return true;
    }

    private bool Pick(int index)
    {
        if (index < 0 || index >= _options.Count)
            return false;

        UpgradeDefinition upgrade = _options[index].Upgrade;

        // Someone else may have just taken an upgrade only one player can have: that option changes
        // and the offer goes back to the owner, who chooses again (C.7).
        if (!_offers.IsStillOfferable(this, _offerLevel, upgrade))
        {
            if (!_offers.Replace(this, _offerLevel, _options, index, "sustituida"))
                _options.RemoveAt(index);

            SendOffer();
            return true;
        }

        // Respiro isn't counted as taken: it can come again whenever nothing else is left.
        if (upgrade != _offers.Pool.Fallback)
            AddStack(upgrade);

        ApplyEffects(upgrade);

        GrantConfirmInvulnerability();
        TargetAnswered(Owner, OfferAnswer.Pick, upgrade.Id, _offerLevel);
        CloseOffer();
        return true;
    }

    // The same level and guarantees, three new options; an evolution stays (C.5). What was on screen
    // weighs half in the new draw.
    private bool Reroll()
    {
        if (!TrySpendReroll())
            return false;

        SetSeen(CurrentIds());
        _offers.Generate(this, _offerLevel, _options, "reroll");
        TargetAnswered(Owner, OfferAnswer.Reroll, null, _offerLevel);
        SendOffer();
        return true;
    }

    // Out of the player's offers for the rest of the match, and another option takes its place (C.5).
    private bool Banish(int index)
    {
        if (index < 0 || index >= _options.Count || !_options[index].CanBanish)
            return false;

        string id = _options[index].Upgrade.Id;
        if (!TryBanish(id))
            return false;

        if (!_offers.Replace(this, _offerLevel, _options, index, "descarte"))
            _options.RemoveAt(index);

        TargetAnswered(Owner, OfferAnswer.Banish, id, _offerLevel);
        SendOffer();
        return true;
    }

    // What the upgrade does (#161): each of its blocks, in order, once for this stack.
    private void ApplyEffects(UpgradeDefinition upgrade)
    {
        EffectContext context = new(this, _stats, _health, upgrade, _offers.Settings);

        foreach (EffectBlock block in upgrade.Effects)
            block?.Acquire(context);
    }

    // A second without hits for having looked at the panel, at most once every 10 (#77): picking three
    // queued offers in a row gives it once.
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
        _offers.Generate(this, _offerLevel, _options, "nueva");
        SendOffer();
    }

    private void CloseOffer()
    {
        SetSeen(CurrentIds());
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
            ids[i] = _options[i].Upgrade != null ? _options[i].Upgrade.Id : "";

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

        UpgradeDefinition[] options = new UpgradeDefinition[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            // Host and clients run the same build, so a missing id is a bug worth seeing.
            if (!offers.Pool.TryGet(ids[i], out options[i]))
                Debug.LogError($"[PlayerUpgrades] The host offered «{ids[i]}», which this build doesn't have.", this);
        }

        CurrentOffer = new ReceivedOffer(offerId, level, options, banishable);
        OfferChanged?.Invoke();
    }

    [TargetRpc]
    private void TargetCloseOffer(NetworkConnection target)
    {
        CurrentOffer = null;
        OfferChanged?.Invoke();
    }

    // The host accepted an answer: the launcher hears it from the player who made it (#78).
    [TargetRpc]
    private void TargetAnswered(NetworkConnection target, OfferAnswer answer, string upgradeId, int level)
    {
        int matchTime = MatchClock.Current != null ? MatchClock.Current.ElapsedSeconds : 0;

        switch (answer)
        {
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

            case OfferAnswer.Banish:
                LauncherEvents.OfferAction("banish", upgradeId, level, matchTime);
                break;

            case OfferAnswer.Skip:
                LauncherEvents.OfferAction("skip", null, level, matchTime);
                break;
        }
    }

    /// <summary>
    /// Server only. Counts an offer for the duo guarantee: after a few in a row that could have had a duo
    /// and didn't, the next one brings one.
    /// </summary>
    public void CountOffer(bool couldHaveDuo, bool hadDuo)
    {
        if (hadDuo)
            _offersWithoutDuo = 0;
        else if (couldHaveDuo)
            _offersWithoutDuo++;
    }

    /// <summary>
    /// Server only. Whether this player meets one requirement of an upgrade, written as in the catalog:
    /// kind:value, or the id of another upgrade they must have. char: is the player's character and
    /// skill:id@n a skill they have at level n or more (#162).
    /// </summary>
    public bool MeetsRequirement(string requirement)
    {
        if (string.IsNullOrEmpty(requirement))
            return true;

        int colon = requirement.IndexOf(':');
        if (colon < 0)
            return GetStacks(requirement) > 0;

        string kind = requirement.Substring(0, colon);
        string value = requirement.Substring(colon + 1);

        switch (kind)
        {
            case "basic":
                return value == "projectile" && _hasProjectileBasic;

            case "char":
                return _character != null && _character.Id == value;

            case "skill":
                return HasSkill(value);

            // The basic attack and the skills that take the caster's element all use the player's
            // element: the character's, or the one F6 set.
            case "element":
            case "basicElement":
                return TryParseElement(value, out ElementType element) && PlayerElementIs(element);

            case "skillElement":
                return TryParseElement(value, out ElementType skillElement) && HasSkillOfElement(skillElement);

            default:
                // canReact, skillLayer, feature... belong to systems that don't exist yet. Never holding
                // is the safe side: an upgrade is never offered without what it needs.
                if (WarnedKinds.Add(kind))
                    Debug.LogWarning($"[PlayerUpgrades] Requirements of kind «{kind}» aren't known yet: {requirement} never holds.", this);

                return false;
        }
    }

    // skill:id, or skill:id@level: the player has it at that level or more (#162).
    private bool HasSkill(string value)
    {
        int at = value.IndexOf('@');
        string id = at < 0 ? value : value.Substring(0, at);
        int level = at < 0 || !int.TryParse(value.Substring(at + 1), out int parsed) ? 1 : parsed;

        return _skills != null && _skills.GetLevel(id) >= level;
    }

    private bool HasSkillOfElement(ElementType element)
    {
        if (_skills == null || _element == null)
            return false;

        for (int slot = (int)AttackSlot.Skill1; slot <= (int)AttackSlot.Skill3; slot++)
        {
            SkillInstance skill = _skills.GetSkill((AttackSlot)slot);
            if (skill != null && skill.GetElement(_element.Current) == element)
                return true;
        }

        return false;
    }

    private bool PlayerElementIs(ElementType element)
    {
        return _element != null && _element.Current == element;
    }

    private static bool TryParseElement(string value, out ElementType element)
    {
        element = value switch
        {
            "fire" => ElementType.Fire,
            "water" => ElementType.Water,
            "electric" => ElementType.Electric,
            _ => ElementType.None,
        };

        return element != ElementType.None;
    }

    // Once per batch of changes, so a HUD listening redraws once.
    private void OnStacksChanged(SyncDictionaryOperation operation, string key, int value, bool asServer)
    {
        if (!asServer && operation == SyncDictionaryOperation.Complete)
            UpgradesChanged?.Invoke();
    }
}
