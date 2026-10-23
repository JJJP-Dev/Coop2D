using System.Collections.Generic;
using UnityEngine;

// An upgrade as data (#78). The fields follow catalog.json in the progression catalog, which is the
// reference: the assets are made from it, and the game never reads the JSON.
// The top part decides whether and how often it is offered (#78); Effects, what it does when taken (#161).
[CreateAssetMenu(fileName = "Upgrade", menuName = "Coop2D/Progression/Upgrade")]
public class UpgradeDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: def_vitality, basic_tempo... It travels over the network " +
             "in the offers and the launcher's events use it, so once a build is out it never changes.")]
    [SerializeField] private string _id;

    [Tooltip("Shown to players.")]
    [SerializeField] private string _displayName;

    [Tooltip("One line, as in the catalog: the panel has room for no more.")]
    [SerializeField] private string _description;

    [Tooltip("Optional, for the panel. Without it the panel shows only the text.")]
    [SerializeField] private Sprite _icon;

    [Header("Offer")]
    [SerializeField] private UpgradeCategory _category;
    [SerializeField] private UpgradeElement _element;
    [SerializeField] private UpgradeRarity _rarity;

    [Tooltip("How many times one player can take it. Each one after the first comes in the «yours» place of the offer.")]
    [SerializeField, Min(1)] private int _maxStacks = 1;

    [Tooltip("How likely it is among the upgrades of its family and rarity. 1 is normal; 0 is never drawn.")]
    [SerializeField, Min(0f)] private float _weight = 1f;

    [Tooltip("Only one player of the group can have it (the duo ones).")]
    [SerializeField] private bool _uniquePerGroup;

    [Tooltip("A risk upgrade: at most one per offer, from level 4.")]
    [SerializeField] private bool _risk;

    [Header("Who can get it")]
    [Tooltip("Id of the character it belongs to (ember, tide, volt, blade). Empty: a generic upgrade, for everyone. " +
             "Character upgrades are the «own» family of the offers.")]
    [SerializeField] private string _character;

    [Tooltip("All of them must hold, as in the catalog: basic:projectile, char:ember, skill:tide_wave, element:fire, " +
             "skillElement:water, basicElement:electric, or the id of another upgrade. One this build doesn't know never holds.")]
    [SerializeField] private string[] _requires = { };

    [Tooltip("Ids of upgrades that can't go with this one. Either side is enough: if A excludes B, B never comes with A.")]
    [SerializeField] private string[] _excludes = { };

    [SerializeField] private UpgradeAppearIf _appearIf = new();

    [Header("What it does")]
    [Tooltip("Applied by the host when a player takes it, once per stack. Add a block with + and pick its kind " +
             "with its button.")]
    [SerializeReference, SubclassPicker] private List<EffectBlock> _effects = new();

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public UpgradeCategory Category => _category;
    public UpgradeElement Element => _element;
    public UpgradeRarity Rarity => _rarity;
    public int MaxStacks => _maxStacks;
    public float Weight => _weight;
    public bool UniquePerGroup => _uniquePerGroup;
    public bool IsRisk => _risk;
    public string Character => _character;
    public bool IsOwnFamily => !string.IsNullOrEmpty(_character);
    public string[] Requires => _requires;
    public string[] Excludes => _excludes;
    public UpgradeAppearIf AppearIf => _appearIf;
    public IReadOnlyList<EffectBlock> Effects => _effects;

    /// <summary>
    /// Whether this upgrade and that one can't go together, from either side.
    /// </summary>
    public bool IsExcludedWith(UpgradeDefinition other)
    {
        return other != null && (Lists(_excludes, other._id) || Lists(other._excludes, _id));
    }

    private static bool Lists(string[] ids, string id)
    {
        if (ids == null || string.IsNullOrEmpty(id))
            return false;

        foreach (string listed in ids)
        {
            if (listed == id)
                return true;
        }

        return false;
    }
}
