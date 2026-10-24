using UnityEngine;

// The numbers of the upgrade offers (#78), from the progression catalog (C). Everything that decides
// what an offer brings is here, so it can be tuned without touching the code.
[CreateAssetMenu(fileName = "OfferSettings", menuName = "Coop2D/Progression/Offer Settings")]
public class OfferSettings : ScriptableObject
{
    [Header("Offer")]
    [Tooltip("Options of each offer.")]
    [SerializeField, Range(1, 4)] private int _choices = 3;

    [Header("Rarity (C.2)")]
    [Tooltip("From the lowest level up. Each row applies from its level until the next row's.")]
    [SerializeField]
    private RarityWeights[] _rarityByLevel =
    {
        new(2, 80, 20, 0),
        new(5, 65, 30, 5),
        new(9, 50, 38, 12),
        new(13, 40, 40, 20),
    };

    [Tooltip("Most luck (rarity_luck) that counts. Each point moves weight from common: two thirds to rare and one to epic.")]
    [SerializeField, Min(0)] private int _maxLuck = 20;

    [Header("Families (C.3)")]
    [Tooltip("Chance that an open place looks among the upgrades of your character instead of the generic ones.")]
    [SerializeField, Range(0f, 1f)] private float _ownFamilyChance = 0.35f;

    [Tooltip("What an upgrade you were offered last time weighs, times its weight.")]
    [SerializeField, Range(0f, 1f)] private float _seenWeight = 0.5f;

    [Header("Guarantees (C.4)")]
    [Tooltip("Level whose offer brings at least a rare in place B. 0: none.")]
    [SerializeField, Min(0)] private int _guaranteedRareLevel = 5;

    [Tooltip("Level whose offer brings an epic (or a duo, from #166) in place B. 0: none.")]
    [SerializeField, Min(0)] private int _guaranteedEpicLevel = 10;

    [Header("Skills (#162, C.1 and C.4)")]
    [Tooltip("Levels whose place B offers a new skill while the player has a free slot: 2 and 6 in the catalog.")]
    [SerializeField] private int[] _skillGuaranteeLevels = { 2, 6 };

    [Tooltip("In an open place, the chance that it offers a new skill instead of an upgrade, while the " +
             "player has a free slot and a skill to be offered. 0.25 in the catalog.")]
    [SerializeField, Range(0f, 1f)] private float _newSkillChance = 0.25f;

    [Tooltip("Weight of a new skill of the player's character, against the generic ones (catalog C.3: 3 to 1).")]
    [SerializeField, Min(0f)] private float _ownSkillWeight = 3f;

    [SerializeField, Min(0f)] private float _genericSkillWeight = 1f;

    [Tooltip("Weight of the next level of a skill, against the upgrades of place A (whose weight is their own).")]
    [SerializeField, Min(0f)] private float _skillLevelWeight = 1f;

    [Tooltip("Playing alone, from this level a skill guarantee only offers skills of another element while " +
             "the player has none (catalog E.0 rule 1), so they can always react on their own. 0: never.")]
    [SerializeField, Min(0)] private int _soloOtherElementLevel = 6;

    [Header("Risk")]
    [Tooltip("First level whose offers can bring a risk upgrade. At most one per offer.")]
    [SerializeField, Min(0)] private int _riskMinLevel = 4;

    [Header("Duo (C.6)")]
    [Tooltip("First level whose offers can bring a duo.")]
    [SerializeField, Min(0)] private int _duoMinLevel = 6;

    [Tooltip("Chance that an offer with a duo possible swaps place C for one.")]
    [SerializeField, Range(0f, 1f)] private float _duoChance = 0.2f;

    [Tooltip("Offers in a row with a duo possible and none in them, after which the next one brings one.")]
    [SerializeField, Min(1)] private int _duoDroughtOffers = 3;

    [Header("Rerolls, banishes and skips (C.5)")]
    [Tooltip("Most rerolls a player gets in a match, from every source together.")]
    [SerializeField, Min(0)] private int _maxRerolls = 2;

    [Tooltip("Pre 3 only: one reroll for every player at each of these match times, in seconds. From the 0.1 they " +
             "come from the first elite and the special room (#92, #90).")]
    [SerializeField] private int[] _timedRerollSeconds = { 240, 480 };

    [Tooltip("Thirds of a reroll for skipping an offer.")]
    [SerializeField, Min(0)] private int _skipRerollThirds = 1;

    [Tooltip("Banishes each player starts the match with.")]
    [SerializeField, Min(0)] private int _banishes = 1;

    [Header("Panel (#77)")]
    [Tooltip("Seconds without taking hits after picking or skipping, for having looked away from the fight.")]
    [SerializeField, Min(0f)] private float _confirmInvulnerability = 1f;

    [Tooltip("Seconds before that invulnerability can come again, so picking several queued offers in a row " +
             "doesn't chain it.")]
    [SerializeField, Min(0f)] private float _confirmInvulnerabilityCooldown = 10f;

    public int Choices => _choices;
    public int MaxLuck => _maxLuck;
    public float OwnFamilyChance => _ownFamilyChance;
    public float SeenWeight => _seenWeight;
    public int GuaranteedRareLevel => _guaranteedRareLevel;
    public int GuaranteedEpicLevel => _guaranteedEpicLevel;
    public float NewSkillChance => _newSkillChance;
    public float OwnSkillWeight => _ownSkillWeight;
    public float GenericSkillWeight => _genericSkillWeight;
    public float SkillLevelWeight => _skillLevelWeight;
    public int SoloOtherElementLevel => _soloOtherElementLevel;

    public bool IsSkillGuaranteeLevel(int level)
    {
        if (_skillGuaranteeLevels == null)
            return false;

        foreach (int guaranteed in _skillGuaranteeLevels)
        {
            if (guaranteed == level)
                return true;
        }

        return false;
    }

    public int RiskMinLevel => _riskMinLevel;
    public int DuoMinLevel => _duoMinLevel;
    public float DuoChance => _duoChance;
    public int DuoDroughtOffers => _duoDroughtOffers;
    public int MaxRerollThirds => _maxRerolls * 3;
    public int[] TimedRerollSeconds => _timedRerollSeconds;
    public int SkipRerollThirds => _skipRerollThirds;
    public int Banishes => _banishes;
    public float ConfirmInvulnerability => _confirmInvulnerability;
    public float ConfirmInvulnerabilityCooldown => _confirmInvulnerabilityCooldown;

    /// <summary>
    /// The weight of each rarity in the offer of this level, with this much luck moved from common.
    /// </summary>
    public int GetRarityWeight(UpgradeRarity rarity, int level, int luck)
    {
        RarityWeights row = GetRow(level);
        if (row == null)
            return rarity == UpgradeRarity.Common ? 1 : 0;

        int common = row.Get(UpgradeRarity.Common);
        int moved = Mathf.Clamp(luck, 0, Mathf.Min(_maxLuck, common));
        int toRare = moved * 2 / 3;

        return rarity switch
        {
            UpgradeRarity.Common => common - moved,
            UpgradeRarity.Rare => row.Get(UpgradeRarity.Rare) + toRare,
            UpgradeRarity.Epic => row.Get(UpgradeRarity.Epic) + moved - toRare,
            _ => row.Get(rarity),
        };
    }

    // The last row that has started by this level. Below the first one, the first one.
    private RarityWeights GetRow(int level)
    {
        if (_rarityByLevel == null || _rarityByLevel.Length == 0)
            return null;

        RarityWeights row = _rarityByLevel[0];
        foreach (RarityWeights candidate in _rarityByLevel)
        {
            if (candidate != null && level >= candidate.FromLevel)
                row = candidate;
        }

        return row;
    }
}
