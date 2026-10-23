using System.Collections.Generic;
using UnityEngine;

// Builds the options of one upgrade offer (#78), following the progression catalog (C.1 to C.6).
// A plain class, not a component: the host creates one and calls it with each player's context and
// offer stream. It draws in a fixed order and goes through the pool in its order, so the same seed with
// the same choices gives the same offers (#75). Changing that order, or the pool's, changes what a seed offers.
//
// Order of the draws for a full offer:
//   1. Place A: one weighted pick among the evolutions, or else among «yours». Nothing when there are none.
//   2. Each open place: a family roll, a rarity roll and a weighted pick. Place B with a guarantee skips
//      the rarity roll when it finds that rarity.
//   3. A duo roll, only when the level allows duos and one could be offered.
public class OfferGenerator
{
    // Weights are whole numbers so the rolls don't depend on how a platform rounds floats:
    // an upgrade of weight 1 weighs 100, and 50 if it was in the last offer.
    private const int WeightScale = 100;

    private readonly UpgradePool _pool;
    private readonly OfferSettings _settings;

    // Reused on every draw, so an offer allocates almost nothing.
    private readonly List<UpgradeDefinition> _candidates = new();
    private readonly List<int> _weights = new();
    private readonly List<UpgradeDefinition> _owned = new();

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
        if (kept.HasValue && IsStillOfferable(context, kept.Value.Upgrade))
            options.Add(kept.Value);
        else if (TryPick(context, random, options, Kind.Evolution, false, UpgradeRarity.Common, out UpgradeDefinition evolution))
            options.Add(new OfferOption(evolution, OfferPlace.Evolution));
        else if (TryPick(context, random, options, Kind.Yours, false, UpgradeRarity.Common, out UpgradeDefinition yours))
            options.Add(new OfferOption(yours, OfferPlace.Yours));

        // 2. The open places. Place B, the second one, carries the level's guarantee.
        while (options.Count < _settings.Choices)
        {
            OfferPlace place = options.Count == 1 && HasGuarantee(context.Level, out _) ? OfferPlace.Guaranteed : OfferPlace.Open;
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
    /// Whether this upgrade could still be offered to this player now. The host checks it again when the
    /// player picks: another player may have taken an upgrade only one can have in the meantime (C.7).
    /// </summary>
    public bool IsStillOfferable(OfferContext context, UpgradeDefinition upgrade)
    {
        if (upgrade != null && upgrade == _pool.Fallback)
            return true;

        CollectOwned(context);
        return IsOfferable(context, upgrade, null);
    }

    private enum Kind
    {
        Evolution,
        Yours,
        Open,
        Duo,
    }

    // An open place, or place B with a guarantee: family, rarity, the weighted pick, and the way down
    // when nothing fits (C.1): lower rarities first, then the other family, then the higher rarities the
    // level allows, then Respiro.
    private bool TryFill(OfferContext context, SeededRandom random, List<OfferOption> options, OfferPlace place,
                         out OfferOption option)
    {
        bool own = random.Chance(_settings.OwnFamilyChance);

        if (place == OfferPlace.Guaranteed && HasGuarantee(context.Level, out UpgradeRarity guaranteed))
        {
            // At least that rarity, in either family. Without any, the place goes back to being open.
            for (int family = 0; family < 2; family++)
            {
                bool ownFamily = family == 0 ? own : !own;
                for (UpgradeRarity rarity = guaranteed; rarity <= UpgradeRarity.Epic; rarity++)
                {
                    if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, out UpgradeDefinition upgrade))
                    {
                        option = new OfferOption(upgrade, OfferPlace.Guaranteed);
                        return true;
                    }
                }
            }
        }

        UpgradeRarity rolled = RollRarity(context, random);

        for (int family = 0; family < 2; family++)
        {
            bool ownFamily = family == 0 ? own : !own;
            for (UpgradeRarity rarity = rolled; rarity >= UpgradeRarity.Common; rarity--)
            {
                if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, out UpgradeDefinition upgrade))
                {
                    option = new OfferOption(upgrade, place == OfferPlace.Guaranteed ? OfferPlace.Open : place);
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

                if (TryPick(context, random, options, Kind.Open, ownFamily, rarity, out UpgradeDefinition upgrade))
                {
                    option = new OfferOption(upgrade, place == OfferPlace.Guaranteed ? OfferPlace.Open : place);
                    return true;
                }
            }
        }

        // Respiro, only once per offer: two of the same would be one choice shown twice.
        if (_pool.Fallback != null && !Contains(options, _pool.Fallback))
        {
            option = new OfferOption(_pool.Fallback, OfferPlace.Fallback);
            return true;
        }

        option = default;
        return false;
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

        if (TryPick(context, random, options, Kind.Duo, false, UpgradeRarity.Common, out UpgradeDefinition duo))
            options.Insert(last, new OfferOption(duo, OfferPlace.Duo));
    }

    /// <summary>
    /// Whether this offer could have had a duo, for the count of offers without one (C.4).
    /// </summary>
    public bool CouldOfferDuo(OfferContext context)
    {
        CollectOwned(context);
        return _settings.Choices >= 2 && context.Level >= _settings.DuoMinLevel && HasCandidates(context, null, Kind.Duo);
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
        if (TryPick(context, random, options, kind, false, UpgradeRarity.Common, out UpgradeDefinition upgrade))
        {
            option = new OfferOption(upgrade, place);
            return true;
        }

        option = default;
        return false;
    }

    // One weighted pick among the upgrades of this kind that can be offered. Uses one number only when
    // there is something to pick.
    private bool TryPick(OfferContext context, SeededRandom random, List<OfferOption> options, Kind kind, bool ownFamily,
                         UpgradeRarity rarity, out UpgradeDefinition picked)
    {
        int total = Gather(context, options, kind, ownFamily, rarity);
        picked = null;

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
        return Gather(context, options, kind, false, UpgradeRarity.Common) > 0;
    }

    // Fills the candidate lists and returns their total weight.
    private int Gather(OfferContext context, List<OfferOption> options, Kind kind, bool ownFamily, UpgradeRarity rarity)
    {
        _candidates.Clear();
        _weights.Clear();
        int total = 0;

        foreach (UpgradeDefinition upgrade in _pool.Upgrades)
        {
            if (!IsKind(context, upgrade, kind, ownFamily, rarity) || !IsOfferable(context, upgrade, options))
                continue;

            float seen = context.Holder.WasSeen(upgrade.Id) ? _settings.SeenWeight : 1f;
            int weight = Mathf.RoundToInt(upgrade.Weight * seen * WeightScale);
            if (weight <= 0)
                continue;

            _candidates.Add(upgrade);
            _weights.Add(weight);
            total += weight;
        }

        return total;
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

    // The filters of C.1: requirements, exclusions, appearIf, stacks, banishes, upgrades only one player
    // can have and at most one risk upgrade per offer. options is null when checking one on its own.
    // Not yet: the cap of each stat (#157) and «what you can't use», which comes with the reaction
    // upgrades (#89).
    private bool IsOfferable(OfferContext context, UpgradeDefinition upgrade, List<OfferOption> options)
    {
        if (upgrade == null || string.IsNullOrEmpty(upgrade.Id) || upgrade == _pool.Fallback)
            return false;

        IUpgradeHolder holder = context.Holder;

        if (options != null && Contains(options, upgrade))
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

    // What the player has, once per draw, for the exclusions.
    private void CollectOwned(OfferContext context)
    {
        _owned.Clear();

        foreach (UpgradeDefinition upgrade in _pool.Upgrades)
        {
            if (upgrade != null && context.Holder.GetStacks(upgrade.Id) > 0)
                _owned.Add(upgrade);
        }
    }

    private static bool Contains(List<OfferOption> options, UpgradeDefinition upgrade)
    {
        foreach (OfferOption option in options)
        {
            if (option.Upgrade == upgrade)
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
