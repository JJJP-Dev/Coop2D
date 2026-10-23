using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The upgrades of one player in this match (#78) and what they have to change their offers with:
// rerolls, counted in thirds, and banishes. The host decides all of it. The upgrades each player has
// travel to every client, because the group's HUD will show them; the rest is the host's alone.
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

    // Server side. Earned counts every third ever received in this match, spent ones included, so the
    // limit of 2 rerolls per match holds whatever the order of earning and spending (catalog C.5).
    private int _earnedThirds;
    private int _spentThirds;
    private readonly HashSet<string> _banished = new();
    private readonly HashSet<string> _seen = new();
    private int _offersWithoutDuo;

    private PlayerIdentity _identity;
    private PlayerElement _element;
    private PlayerSkills _skills;
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
        TryGetComponent(out _skills);

        // Every basic attack is a projectile until the melee basic arrives (#96).
        _hasProjectileBasic = GetComponent<PlayerAutoAttack>() != null;
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
        _seen.Clear();
        _offersWithoutDuo = 0;
        _earnedThirds = 0;
        _spentThirds = 0;
        _rerollThirds.Value = 0;
        _banishesLeft.Value = Mathf.Max(0, banishes);
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
    /// kind:value, or the id of another upgrade they must have. Characters arrive with #162, so for now
    /// char: never holds, and the skills are the test ones, so skill: only holds for those.
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
                return false;

            case "skill":
                return HasSkill(value);

            // Until characters exist, the basic attack and the skills that take the caster's element
            // all use the player's element.
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

    // skill:id, or skill:id@level. Every skill is at level 1 until skill levels arrive (#162).
    private bool HasSkill(string value)
    {
        int at = value.IndexOf('@');
        string id = at < 0 ? value : value.Substring(0, at);
        int level = at < 0 || !int.TryParse(value.Substring(at + 1), out int parsed) ? 1 : parsed;

        if (_skills == null || level > 1)
            return false;

        for (int slot = (int)AttackSlot.Skill1; slot <= (int)AttackSlot.Skill3; slot++)
        {
            SkillDefinition skill = _skills.GetSkill((AttackSlot)slot);
            if (skill != null && skill.Id == id)
                return true;
        }

        return false;
    }

    private bool HasSkillOfElement(ElementType element)
    {
        if (_skills == null || _element == null)
            return false;

        for (int slot = (int)AttackSlot.Skill1; slot <= (int)AttackSlot.Skill3; slot++)
        {
            SkillDefinition skill = _skills.GetSkill((AttackSlot)slot);
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
