using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// What one player has gathered in this match to choose their upgrades with: the upgrades taken, the
// rerolls (counted in thirds) and banishes, and what the last offer showed. The host decides all of it;
// the stacks travel to every client, because the group's HUD will show them.
// The offers themselves, and the conversation with the owner about them, are PlayerOffers.
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

    // Server side. Earned counts every third ever received in this match, spent ones included, so the
    // limit of 2 rerolls per match holds whatever the order of earning and spending (catalog C.5).
    private int _earnedThirds;
    private int _spentThirds;
    private readonly HashSet<string> _banished = new();
    private readonly HashSet<string> _seen = new();
    private int _offersWithoutDuo;

    private PlayerIdentity _identity;
    private PlayerElement _element;
    private PlayerCharacter _character;
    private PlayerSkillSlots _skills;
    private PlayerStats _stats;
    private Health _health;
    private PlayerUpgradeEvents _events;
    private UpgradeRequirements _requirements;

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

        // Every basic attack is a projectile until the melee basic arrives.
        bool hasProjectileBasic = GetComponent<PlayerAutoAttack>() != null;
        _requirements = new UpgradeRequirements(this, _character, _element, _skills, hasProjectileBasic);
    }

    public int GetStacks(string id)
    {
        return !string.IsNullOrEmpty(id) && _stacks.TryGetValue(id, out int stacks) ? stacks : 0;
    }

    // --- Skills in the offers (#162) ---

    public ElementType Element => _element != null ? _element.Current : ElementType.None;

    public bool HasFreeSkillSlot => _skills != null && _skills.HasFreeSlot;

    public int GetSkillLevel(string skillId)
    {
        return _skills != null ? _skills.GetLevel(skillId) : 0;
    }

    public void CollectOwnedSkills(List<SkillDefinition> skills)
    {
        skills.Clear();
        if (_skills == null)
            return;

        for (int slot = 0; slot < _skills.Count; slot++)
        {
            SkillInstance skill = _skills.Get(slot);
            if (skill != null)
                skills.Add(skill.Definition);
        }
    }

    public void CollectSkillPool(List<SkillDefinition> own, List<SkillDefinition> generic)
    {
        own.Clear();
        generic.Clear();

        CharacterDefinition character = _character != null ? _character.Definition : null;
        if (character != null)
            own.AddRange(character.OwnSkills);

        if (_skills != null && _skills.Library != null)
            generic.AddRange(_skills.Library.Generic);
    }

    /// <summary>
    /// Server only. Nothing taken, nothing banished, no rerolls: how every player starts a match.
    /// PlayerOffers calls it, as it also empties the offer queue.
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
    }

    /// <summary>
    /// Server only. The player takes this upgrade from an offer: one more stack (unless it doesn't count,
    /// like the fallback) and what its effects give, once for this stack.
    /// </summary>
    public void Acquire(UpgradeDefinition upgrade, bool countStack, OfferSettings settings)
    {
        if (!IsServerInitialized || upgrade == null)
            return;

        if (countStack)
            AddStack(upgrade);

        EffectContext context = new(this, _stats, _health, upgrade, settings);
        foreach (EffectBlock block in upgrade.Effects)
            block?.Acquire(context);
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
    /// Server only. Takes this option (an upgrade's id or a skill's key) out of the player's offers for the
    /// rest of the match.
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
    /// Server only. Whether this player meets one requirement of an upgrade (UpgradeRequirements).
    /// </summary>
    public bool MeetsRequirement(string requirement)
    {
        return _requirements.Meets(requirement);
    }

    // Once per batch of changes, so a HUD listening redraws once.
    private void OnStacksChanged(SyncDictionaryOperation operation, string key, int value, bool asServer)
    {
        if (!asServer && operation == SyncDictionaryOperation.Complete)
            UpgradesChanged?.Invoke();
    }
}
