using UnityEngine;

// A skill as data. The fields follow skills.json in the progression catalog,
// so its entries can be turned into these assets later (#162).
[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Coop2D/Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: ember_flare, tide_wave...")]
    [SerializeField] private string _id;

    [Tooltip("Shown to players.")]
    [SerializeField] private string _displayName;

    [Tooltip("For the HUD (#73). Optional: without it the HUD shows the first letter of the name.")]
    [SerializeField] private Sprite _icon;

    [Header("Element")]
    [Tooltip("The catalog's \"primary\": the skill takes the element of whoever casts it, and Element is ignored.")]
    [SerializeField] private bool _usesCasterElement;

    [SerializeField] private ElementType _element;

    [Tooltip("Only skills that can detonate trigger reactions. The rest leave their aura and never detonate.")]
    [SerializeField] private bool _canDetonate = true;

    [SerializeField] private AuraStrength _auraStrength = AuraStrength.Strong;

    [Header("Use")]
    [Tooltip("Seconds.")]
    [SerializeField, Min(0.1f)] private float _cooldown = 7f;

    [Tooltip("How far from the caster it can land, in units. Aiming farther drops it at this distance.")]
    [SerializeField, Min(0f)] private float _castRange = 7f;

    [Header("Effect")]
    [SerializeField, Min(0)] private int _damage = 16;

    [Tooltip("Radius of the circle, in units. The catalog asks for at least 1.5.")]
    [SerializeField, Min(0.1f)] private float _radius = 1.5f;

    [Tooltip("What players see where it lands. Only the picture: it never decides hits.")]
    [SerializeField] private SkillCircleVisual _visualPrefab;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public bool CanDetonate => _canDetonate;
    public AuraStrength AuraStrength => _auraStrength;
    public float Cooldown => _cooldown;
    public float CastRange => _castRange;
    public int Damage => _damage;
    public float Radius => _radius;
    public SkillCircleVisual VisualPrefab => _visualPrefab;

    public ElementType GetElement(ElementType casterElement)
    {
        return _usesCasterElement ? casterElement : _element;
    }
}
