using UnityEngine;

// A skill as data. The fields follow skills.json in the progression catalog,
// so its entries can be turned into these assets later (#162).
[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Coop2D/Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: ember_flare, tide_wave... The launcher's events use it " +
             "(ability_cast), so once a build is out it never changes.")]
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

    [Tooltip("Circle only: how far from the caster it can land, in units. Aiming farther drops it at this " +
             "distance. Lines and cones start at the caster, so their reach is their Length.")]
    [SerializeField, Min(0f)] private float _castRange = 7f;

    [Header("Shape (#162)")]
    [Tooltip("Circle: where you aim. Line and Cone: from you towards the cursor, always at full length.")]
    [SerializeField] private SkillShape _shape = SkillShape.Circle;

    [Tooltip("Circle only: radius in units. The catalog asks for at least 1.5.")]
    [SerializeField, Min(0.1f)] private float _radius = 1.5f;

    [Tooltip("Line and Cone: how far they reach from the caster, in units.")]
    [SerializeField, Min(0.1f)] private float _length = 5f;

    [Tooltip("Line only: total width of the band, in units. Nothing asks for more precision than the basic " +
             "attack (catalog E.0), so keep it at 0.5 or more.")]
    [SerializeField, Min(0.1f)] private float _width = 0.5f;

    [Tooltip("Cone only: full opening in degrees, from one edge to the other. At most 180.")]
    [SerializeField, Range(10f, 180f)] private float _angle = 60f;

    [Header("Effect")]
    [SerializeField, Min(0)] private int _damage = 16;

    [Tooltip("Units each enemy hit is pushed straight away from the caster. 0: no push. Bosses and " +
             "champions won't be moved (catalog E.0 rule 10) once they exist.")]
    [SerializeField, Min(0f)] private float _knockback;

    [Tooltip("Seconds the push takes.")]
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f;

    [Tooltip("What players see where it lands. Only the picture: it never decides hits.")]
    [SerializeField] private SkillShapeVisual _visual;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public bool CanDetonate => _canDetonate;
    public AuraStrength AuraStrength => _auraStrength;
    public float Cooldown => _cooldown;
    public float CastRange => _castRange;
    public SkillShape Shape => _shape;
    public float Radius => _radius;
    public float Length => _length;
    public float Width => _width;
    public float Angle => _angle;
    public int Damage => _damage;
    public float Knockback => _knockback;
    public float KnockbackDuration => _knockbackDuration;
    public SkillShapeVisual Visual => _visual;

    /// <summary>
    /// Lines and cones start at the caster and point at the cursor; a circle lands on it.
    /// </summary>
    public bool IsAimedByDirection => _shape != SkillShape.Circle;

    public ElementType GetElement(ElementType casterElement)
    {
        return _usesCasterElement ? casterElement : _element;
    }
}
