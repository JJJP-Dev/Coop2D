using System.Collections.Generic;
using UnityEngine;

// Builds the options of one upgrade offer (#78), following the progression catalog (C.1 to C.6).
// A plain class, not a component: the host creates one and calls it with each player's context and
// offer stream. It draws in a fixed order and goes through the pool in its order, so the same seed with
// the same choices gives the same offers (#75). Changing that order, or the pool's, changes what a seed offers.
// Since #162 an option can also be a new skill or the next level of one the player has.
//
// Order of the draws for a full offer:
//   1. Place A: one weighted pick among the evolutions, or else among «yours» (upgrades and the next level
//      of your skills). Nothing when there are none.
//   2. Each open place: on a skill guarantee level, place B picks a new skill. Otherwise a type roll (a new
//      skill, only while one can be offered), a family roll, a rarity roll and a weighted pick. Place B with
//      a rarity guarantee skips the type and rarity rolls when it finds that rarity.
//   3. A duo roll, only when the level allows duos and one could be offered.
public class OfferGenerator
{
    // Weights are whole numbers so the rolls don't depend on how a platform rounds floats:
    // an upgrade of weight 1 weighs 100, and 50 if it was in the last offer.
    private const int WeightScale = 100;

    private readonly UpgradePool _pool;
    private readonly OfferSettings _settings;

    // Reused on every draw, so an offer allocates almost nothing.
    private readonly List<OfferOption> _candidates = new();
    private readonly List<int> _weights = new();
    private readonly List<UpgradeDefinition> _owned = new();
    private readonly List<SkillDefinition> _ownedSkills = new();
    private readonly List<SkillDefinition> _ownSkills = new();
    private readonly List<SkillDefinition> _genericSkills = new();

    public OfferGenerator(UpgradePool pool, OfferSettings settings)
    {
        _pool = pool;
        _settings = settings;
    }

    /// <summary>
    /// Fills options with a whole offer. For a reroll, pass the offer being rerolled in options: its
    /// evolution stays in place A (C.5). The offer can come out shorter than the settings ask when there
    /// isn't even Respiro for every place: Respiro comes at most once.
    /// </summary>
    public void Generate(OfferContext context, SeededRandom random, List<OfferOption> options)
    {
        OfferOption? kept = null;
        foreach (OfferOption option in options)
        {
            if (option.IsLocked)
                kept = option;
        }

        options.Clear();
        CollectOwned(context);

        // 1. Place A.
        if (kept.HasValue && IsStillOfferable(context, kept.Value))
            options.Add(kept.Value);
        else if (TryPick(context, random, options, Kind.Evolution, false, UpgradeRarity.Common, false, out OfferOption evolution))
            options.Add(evolution.WithPlace(OfferPlace.Evolution));
        else if (TryPick(context, random, options, Kind.Yours, false, UpgradeRarity.Common, false, out OfferOption yours))
            options.Add(yours.WithPlace(OfferPlace.Yours));

        // 2. The open places. Place B, the second one, carries the level's guarantee.
        while (options.Count < _settings.Choices)
        {
            OfferPlace place = options.Count == 1 && HasAnyGuarantee(context.Level) ? OfferPlace.Guaranteed : OfferPlace.Open;
            if (!TryFill(context, random, options, place, out OfferOption option))
                break;

            options.Add(option);
        }

        // 3. The duo, which takes place C.
        TrySwapInDuo(context, random, options);
    }

    /// <summary>
    /// Replaces the option at this index by another one of the same place, for a banish or for an option
    /// that stopped being valid. False when the place can't be replaced or nothing fits, Respiro included.
    /// </summary>
    public bool Replace(OfferContext context, SeededRandom random, List<OfferOption> options, int index)
    {
        if (index < 0 || index >= options.Count)
            return false;

        OfferPlace place = options[index].Place;
        if (place == OfferPlace.Evolution)
            return false;

        CollectOwned(context);

        // Out of the offer while its replacement is drawn, so the rest still count as taken.
        options.RemoveAt(index);

        OfferOption replacement = default;
        bool found = place switch
        {
            OfferPlace.Yours => TryPickAs(context, random, options, Kind.Yours, OfferPlace.Yours, out replacement)
                                || TryFill(context, random, options, OfferPlace.Open, out replacement),
            OfferPlace.Duo => TryPickAs(context, random, options, Kind.Duo, OfferPlace.Duo, out replacement)
                              || TryFill(context, random, options, OfferPlace.Open, out replacement),
            _ => TryFill(context, random, options, place, out replacement),
        };

        if (!found)
            return false;

        options.Insert(index, replacement);
        return true;
    }

    /// <summary>
    /// Whether this option could still be offered to this player now. The host checks it again when the
    /// player picks: another player may have taken an upgrade only one can have in the meantime (C.7).
    /// </summary>
    public bool IsStillOfferable(OfferContext context, OfferOption option)
    {
        if (option.IsSkill)
            return IsSkillOfferable(context, option.Skill, option.SkillLevel, null);

        if (option.Upgrade != null && option.Upgrade == _pool.Fallback)
            return true;

        CollectOwned(context);
        return IsOfferable(context, option.Upgrade, null);
    }

    private enum Kind
    {
        Evolution,
        Yours,
        Open,
        Duo,
        NewSkill,
    }

    // An open place, or place B with a guarantee: the skill guarantee, then type, family, rarity, the
    // weighted pick, and the way down when nothing fits (C.1): lower rarities first, then the other family,
    // then the higher rarities the level allows, then Respiro.
    private bool TryFill(OfferContext context, SeededRandom random, List<OfferOption> options, OfferPlace place,
                         out OfferOption option)
    {
        // C.4: a new skill on levels 2 and 6 while there's a free slot. Without one, the place is open.
        if (place == OfferPlace.Guaranteed && _settings.IsSkillGuaranteeLevel(context.Level) &&
            TryPickNewSkill(context, random, options, NeedsOtherElement(context), out option))
        {
            option = option.WithPlace(OfferPlace.Guaranteed);
            return true;
        }

        // Declared apart: with && the out is only written when place is Guaranteed, and C# can't tell it's
        // never read otherwise.
        UpgradeRarity guaranteed = UpgradeRarity.Common;
        bool rarityGuarantee = place == OfferPlace.Guaranteed && HasGuarantee(context.Level, out guaranteed);

        // C.1: the type first, a new skill a quarter of the time. Rolled only when one could be offered, so
        // a player with no room for skills draws exactly as before #162.
        if (!rarityGuarantee && HasCandidates(context, options, Kind.NewSkill) && random.Chance(_settings.NewSkillChance) &&
            TryPickNewSkill(context, random, options, false, out option))
        {
            option = option.WithPlace(OfferPlace.Open);
            return true;
        }

        bool own = random.Chance(_settings.OwnFamilyChance);

        if (rarityGuarantee)
        {
            // At least that rarity, in either family. Without any, the place goes back to being open.
            for (int family = 0; family < 2; family++)
            {
                bool ownFamily = family == 0 ? own : !own;
                for (UpgradeRarity rarity = guaranteed; rarity <= UpgradeRarity.Epic; rarity++)
                {
                    if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, false, out option))
                    {
                        option = option.WithPlace(OfferPlace.Guaranteed);
                        return true;
                    }
                }
            }
        }

        OfferPlace open = place == OfferPlace.Guaranteed ? OfferPlace.Open : place;
        UpgradeRarity rolled = RollRarity(context, random);

        for (int family = 0; family < 2; family++)
        {
            bool ownFamily = family == 0 ? own : !own;
            for (UpgradeRarity rarity = rolled; rarity >= UpgradeRarity.Common; rarity--)
            {
                if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, false, out option))
                {
                    option = option.WithPlace(open);
                    return true;
                }
            }
        }

        // Nothing at or below the rolled rarity: the higher ones this level allows, before giving Respiro.
        // Without this, a lone epic left in the pool would never come out unless the roll said epic.
        for (int family = 0; family < 2; family++)
        {
            bool ownFamily = family == 0 ? own : !own;
            for (UpgradeRarity rarity = rolled + 1; rarity <= UpgradeRarity.Legendary; rarity++)
            {
                if (_settings.GetRarityWeight(rarity, context.Level, context.Holder.Luck) <= 0)
                    continue;

                if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, false, out option))
                {
                    option = option.WithPlace(open);
                    return true;
                }
            }
        }

        // Respiro, only once per offer: two of the same would be one choice shown twice.
        if (_pool.Fallback != null && !Contains(options, _pool.Fallback.Id))
        {
            option = new OfferOption(_pool.Fallback, OfferPlace.Fallback);
            return true;
        }

        option = default;
        return false;
    }

    // A new skill, the character's weighing more than the generic ones (C.3). With otherElement, only skills
    // of another element than the player's, when there are any.
    private bool TryPickNewSkill(OfferContext context, SeededRandom random, List<OfferOption> options, bool otherElement,
                                 out OfferOption option)
    {
        if (otherElement && TryPick(context, random, options, Kind.NewSkill, false, UpgradeRarity.Common, true, out option))
            return true;

        return TryPick(context, random, options, Kind.NewSkill, false, UpgradeRarity.Common, false, out option);
    }

    // Catalog E.0 rule 1: alone, from level 6, a player without a skill of another element is only
    // guaranteed those, so they can always set off a reaction on their own.
    private bool NeedsOtherElement(OfferContext context)
    {
        if (context.Players > 1 || _settings.SoloOtherElementLevel <= 0 || context.Level < _settings.SoloOtherElementLevel)
            return false;

        foreach (SkillDefinition skill in _ownedSkills)
        {
            if (skill.GetElement(context.Holder.Element) != context.Holder.Element)
                return false;
        }

        return true;
    }

    // From the duo level, while a duo could be offered: a 20 % chance, or for sure after 3 offers in a
    // row without one (C.4, C.6). It takes the last place, C. Nothing happens until the pool has duos (#166).
    private void TrySwapInDuo(OfferContext context, SeededRandom random, List<OfferOption> options)
    {
        if (_settings.Choices < 2 || context.Level < _settings.DuoMinLevel)
            return;

        if (!HasCandidates(context, options, Kind.Duo))
            return;

        bool forced = context.Holder.OffersWithoutDuo >= _settings.DuoDroughtOffers;
        bool rolled = random.Chance(_settings.DuoChance);
        if (!forced && !rolled)
            return;

        int last = Mathf.Min(options.Count, _settings.Choices - 1);
        if (last < options.Count)
            options.RemoveAt(last);

        if (TryPick(context, random, options, Kind.Duo, false, UpgradeRarity.Common, false, out OfferOption duo))
            options.Insert(last, duo.WithPlace(OfferPlace.Duo));
    }

    /// <summary>
    /// Whether this offer could have had a duo, for the count of offers without one (C.4).
    /// </summary>
    public bool CouldOfferDuo(OfferContext context)
    {
        CollectOwned(context);
        return _settings.Choices >= 2 && context.Level >= _settings.DuoMinLevel && HasCandidates(context, null, Kind.Duo);
    }

    private bool HasAnyGuarantee(int level)
    {
        return _settings.IsSkillGuaranteeLevel(level) || HasGuarantee(level, out _);
    }

    private bool HasGuarantee(int level, out UpgradeRarity rarity)
    {
        // Epic on 10 before rare on 5, in case someone sets both to the same level.
        if (_settings.GuaranteedEpicLevel > 0 && level == _settings.GuaranteedEpicLevel)
        {
            rarity = UpgradeRarity.Epic;
            return true;
        }

        if (_settings.GuaranteedRareLevel > 0 && level == _settings.GuaranteedRareLevel)
        {
            rarity = UpgradeRarity.Rare;
            return true;
        }

        rarity = UpgradeRarity.Common;
        return false;
    }

    private UpgradeRarity RollRarity(OfferContext context, SeededRandom random)
    {
        int luck = context.Holder.Luck;
        int total = 0;
        for (UpgradeRarity rarity = UpgradeRarity.Common; rarity <= UpgradeRarity.Legendary; rarity++)
            total += Mathf.Max(0, _settings.GetRarityWeight(rarity, context.Level, luck));

        if (total <= 0)
            return UpgradeRarity.Common;

        int roll = random.Range(0, total);
        for (UpgradeRarity rarity = UpgradeRarity.Common; rarity <= UpgradeRarity.Legendary; rarity++)
        {
            roll -= Mathf.Max(0, _settings.GetRarityWeight(rarity, context.Level, luck));
            if (roll < 0)
                return rarity;
        }

        return UpgradeRarity.Common;
    }

    private bool TryPickAs(OfferContext context, SeededRandom random, List<OfferOption> options, Kind kind,
                           OfferPlace place, out OfferOption option)
    {
        if (TryPick(context, random, options, kind, false, UpgradeRarity.Common, false, out option))
        {
            option = option.WithPlace(place);
            return true;
        }

        return false;
    }

    // One weighted pick among the options of this kind that can be offered. Uses one number only when
    // there is something to pick.
    private bool TryPick(OfferContext context, SeededRandom random, List<OfferOption> options, Kind kind, bool ownFamily,
                         UpgradeRarity rarity, bool otherElement, out OfferOption picked)
    {
        int total = Gather(context, options, kind, ownFamily, rarity, otherElement);
        picked = default;

        if (total <= 0)
            return false;

        int roll = random.Range(0, total);
        for (int i = 0; i < _candidates.Count; i++)
        {
            roll -= _weights[i];
            if (roll < 0)
            {
                picked = _candidates[i];
                return true;
            }
        }

        picked = _candidates[_candidates.Count - 1];
        return true;
    }

    private bool HasCandidates(OfferContext context, List<OfferOption> options, Kind kind)
    {
        return Gather(context, options, kind, false, UpgradeRarity.Common, false) > 0;
    }

    // Fills the candidate lists and returns their total weight. Upgrades go first, in the pool's order;
    // then the skills, in slot order or in the character's and the library's order.
    private int Gather(OfferContext context, List<OfferOption> options, Kind kind, bool ownFamily, UpgradeRarity rarity,
                       bool otherElement)
    {
        _candidates.Clear();
        _weights.Clear();
        int total = 0;

        if (kind != Kind.NewSkill)
        {
            foreach (UpgradeDefinition upgrade in _pool.Upgrades)
            {
                if (!IsKind(context, upgrade, kind, ownFamily, rarity) || !IsOfferable(context, upgrade, options))
                    continue;

                total += AddCandidate(context, new OfferOption(upgrade, OfferPlace.Open), upgrade.Weight);
            }
        }

        // Place A: the next level of each skill the player has (C.1, catalog E.0 rule 5).
        if (kind == Kind.Yours)
        {
            foreach (SkillDefinition skill in _ownedSkills)
            {
                int next = context.Holder.GetSkillLevel(skill.Id) + 1;
                if (IsSkillOfferable(context, skill, next, options))
                    total += AddCandidate(context, new OfferOption(skill, next, OfferPlace.Yours), _settings.SkillLevelWeight);
            }
        }

        if (kind == Kind.NewSkill)
        {
            context.Holder.CollectSkillPool(_ownSkills, _genericSkills);
            total += GatherNewSkills(context, options, _ownSkills, _settings.OwnSkillWeight, otherElement);
            total += GatherNewSkills(context, options, _genericSkills, _settings.GenericSkillWeight, otherElement);
        }

        return total;
    }

    private int GatherNewSkills(OfferContext context, List<OfferOption> options, List<SkillDefinition> skills, float weight,
                                bool otherElement)
    {
        int total = 0;
        foreach (SkillDefinition skill in skills)
        {
            if (skill == null || !IsSkillOfferable(context, skill, 1, options))
                continue;

            if (otherElement && skill.GetElement(context.Holder.Element) == context.Holder.Element)
                continue;

            total += AddCandidate(context, new OfferOption(skill, 1, OfferPlace.Open), weight);
        }

        return total;
    }

    // What the last offer showed weighs half (C.1).
    private int AddCandidate(OfferContext context, OfferOption option, float weight)
    {
        float seen = context.Holder.WasSeen(option.Key) ? _settings.SeenWeight : 1f;
        int scaled = Mathf.RoundToInt(weight * seen * WeightScale);
        if (scaled <= 0)
            return 0;

        _candidates.Add(option);
        _weights.Add(scaled);
        return scaled;
    }

    private bool IsKind(OfferContext context, UpgradeDefinition upgrade, Kind kind, bool ownFamily, UpgradeRarity rarity)
    {
        if (upgrade == null)
            return false;

        return kind switch
        {
            Kind.Evolution => upgrade.Rarity == UpgradeRarity.Evolution,
            Kind.Duo => upgrade.Rarity == UpgradeRarity.Duo,
            Kind.Yours => upgrade.Rarity != UpgradeRarity.Evolution && upgrade.Rarity != UpgradeRarity.Duo && IsYours(context, upgrade),
            _ => upgrade.Rarity == rarity && upgrade.IsOwnFamily == ownFamily,
        };
    }

    // C.1: another stack of an upgrade you have, or one that asks for something of yours: your
    // character, a skill, your element or another upgrade. «basic:projectile» doesn't count: until the
    // melee basic (#96) everyone has it, and it would fill place A for everybody.
    private static bool IsYours(OfferContext context, UpgradeDefinition upgrade)
    {
        if (context.Holder.GetStacks(upgrade.Id) > 0)
            return true;

        if (upgrade.Requires == null)
            return false;

        foreach (string requirement in upgrade.Requires)
        {
            if (string.IsNullOrEmpty(requirement))
                continue;

            int colon = requirement.IndexOf(':');
            string kind = colon < 0 ? null : requirement.Substring(0, colon);

            if (kind == null || kind == "char" || kind == "skill" || kind == "element" ||
                kind == "basicElement" || kind == "skillElement")
                return true;
        }

        return false;
    }

    // A new skill (level 1) while there's room and the player doesn't have it, or the level right after
    // the one they have, up to the skill's maximum. Banished keys are out, and an option is never twice.
    private static bool IsSkillOfferable(OfferContext context, SkillDefinition skill, int level, List<OfferOption> options)
    {
        if (skill == null || string.IsNullOrEmpty(skill.Id) || level < 1 || level > skill.MaxLevel)
            return false;

        IUpgradeHolder holder = context.Holder;
        string key = OfferOption.SkillKey(skill.Id, level);
        if (holder.IsBanished(key) || (options != null && Contains(options, key)))
            return false;

        int has = holder.GetSkillLevel(skill.Id);
        return level == 1 ? has == 0 && holder.HasFreeSkillSlot : has == level - 1;
    }

    // The filters of C.1: requirements, exclusions, appearIf, stacks, banishes, upgrades only one player
    // can have and at most one risk upgrade per offer, and none without effects. options is null when
    // checking one on its own.
    // Not yet: the cap of each stat (#157) and «what you can't use», which comes with the reaction
    // upgrades (#89).
    private bool IsOfferable(OfferContext context, UpgradeDefinition upgrade, List<OfferOption> options)
    {
        if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || upgrade == _pool.Fallback)
            return false;

        // An upgrade with no effect blocks would do nothing if taken (#162): the character ones wait
        // for their blocks. It's checked before any draw, so a seed offers what it did before.
        if (upgrade.Effects == null || upgrade.Effects.Count == 0)
            return false;

        IUpgradeHolder holder = context.Holder;

        if (options != null && Contains(options, upgrade.Id))
            return false;

        if (holder.GetStacks(upgrade.Id) >= upgrade.MaxStacks || holder.IsBanished(upgrade.Id))
            return false;

        if (upgrade.AppearIf != null && !upgrade.AppearIf.Allows(context.Level, context.Players))
            return false;

        if (upgrade.Requires != null)
        {
            foreach (string requirement in upgrade.Requires)
            {
                if (!holder.MeetsRequirement(requirement))
                    return false;
            }
        }

        foreach (UpgradeDefinition owned in _owned)
        {
            if (upgrade.IsExcludedWith(owned))
                return false;
        }

        if (upgrade.UniquePerGroup && context.IsTakenByOther != null && context.IsTakenByOther(upgrade))
            return false;

        if (upgrade.IsRisk && (context.Level < _settings.RiskMinLevel || (options != null && HasRisk(options))))
            return false;

        return true;
    }

    // What the player has, once per draw, for the exclusions and for place A.
    private void CollectOwned(OfferContext context)
    {
        _owned.Clear();

        foreach (UpgradeDefinition upgrade in _pool.Upgrades)
        {
            if (upgrade != null && context.Holder.GetStacks(upgrade.Id) > 0)
                _owned.Add(upgrade);
        }

        context.Holder.CollectOwnedSkills(_ownedSkills);
    }

    private static bool Contains(List<OfferOption> options, string key)
    {
        foreach (OfferOption option in options)
        {
            if (option.Key == key)
                return true;
        }

        return false;
    }

    private static bool HasRisk(List<OfferOption> options)
    {
        foreach (OfferOption option in options)
        {
            if (option.Upgrade != null && option.Upgrade.IsRisk)
                return true;
        }

        return false;
    }
}
