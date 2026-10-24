using UnityEngine;

// A skill as data. The fields follow skills.json in the progression catalog,
// so its entries can be turned into these assets later.
[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Coop2D/Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: ember_flare, tide_wave... The launcher's events use it " +
             "(ability_cast), so once a build is out it never changes.")]
    [SerializeField] private string _id;

    [Tooltip("Shown to players.")]
    [SerializeField] private string _displayName;

    [Tooltip("For the HUD. Optional: without it the HUD shows the first letter of the name.")]
    [SerializeField] private Sprite _icon;

    [Tooltip("One line for the offer of a new skill: «Bola que explota en 1,5 m y detona».")]
    [SerializeField] private string _description;

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

    [Tooltip("Circle, Wall and Turret: how far from the caster it can land, in units. Aiming farther drops it " +
             "at this distance. Lines and cones start at the caster, so their reach is their Length.")]
    [SerializeField, Min(0f)] private float _castRange = 7f;

    [Header("Shape")]
    [Tooltip("Circle: where you aim. Line and Cone: from you towards the cursor, always at full length. " +
             "Projectile: flies from you towards the cursor, up to Length. Wall and Turret: placed where you " +
             "aim, and they stay (see Placed).")]
    [SerializeField] private SkillShape _shape = SkillShape.Circle;

    [Tooltip("Circle: radius in units; the catalog asks for at least 1.5. Turret: how far it reaches.")]
    [SerializeField, Min(0.1f)] private float _radius = 1.5f;

    [Tooltip("Line and Cone: how far they reach from the caster, in units. Projectile: how far it flies. " +
             "Wall: how long the wall is.")]
    [SerializeField, Min(0.1f)] private float _length = 5f;

    [Tooltip("Line and Wall: total width of the band, in units. Nothing asks for more precision than the " +
             "basic attack, so keep it at 0.5 or more.")]
    [SerializeField, Min(0.1f)] private float _width = 0.5f;

    [Tooltip("Cone only: full opening in degrees, from one edge to the other. At most 180.")]
    [SerializeField, Range(10f, 180f)] private float _angle = 60f;

    [Header("Projectile")]
    [Tooltip("Units per second.")]
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 14f;

    [Tooltip("Collision radius of the projectile, in units. Also how wide its path is when the host checks a hit.")]
    [SerializeField, Min(0.05f)] private float _projectileRadius = 0.2f;

    [Tooltip("0: it hits enemies directly, as many as 1 + Pierce. More than 0: it bursts on the first enemy " +
             "it touches and hits everything in a circle of this radius, in units.")]
    [SerializeField, Min(0f)] private float _impactRadius;

    [Tooltip("Hits directly only: how many enemies it goes through before stopping. Rayo: 1, so it hits two.")]
    [SerializeField, Min(0)] private int _pierce;

    [Tooltip("Bursts only: whether it also bursts where it stops when it touched nobody.")]
    [SerializeField] private bool _burstAtEnd = true;

    [Tooltip("Bursts only: seconds the enemy it touched first can't move or attack (Burbuja). 0: none. " +
             "Bosses and champions won't be held once they exist.")]
    [SerializeField, Min(0f)] private float _trapDuration;

    [Tooltip("What flies. Only the picture: every PC flies its own copy, and only the caster's reports hits.")]
    [SerializeField] private SkillProjectile _projectilePrefab;

    [Tooltip("Whether it leaves a trail of its colour, if the prefab has one. Off for slow ones (Burbuja), " +
             "so fast and slow projectiles read apart.")]
    [SerializeField] private bool _projectileTrail = true;

    [Header("Placed (Wall, Turret)")]
    [Tooltip("Seconds it stays.")]
    [SerializeField, Min(0.1f)] private float _duration = 3f;

    [Tooltip("Seconds between its hits. Damage is per hit: a wall of 3 every 0.5 s burns 6 per second.")]
    [SerializeField, Min(0.1f)] private float _hitInterval = 0.5f;

    [Header("Effect")]
    [Tooltip("Per hit: each enemy a cast or a projectile hits, or each hit of a wall or a turret.")]
    [SerializeField, Min(0)] private int _damage = 16;

    [Tooltip("Units each enemy hit is pushed straight away from the caster. 0: no push. Bosses and " +
             "champions won't be moved once they exist.")]
    [SerializeField, Min(0f)] private float _knockback;

    [Tooltip("Seconds the push takes.")]
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f;

    [Tooltip("What players see where it lands. Only the picture: it never decides hits.")]
    [SerializeField] private SkillShapeVisual _visual;

    [Header("Levels")]
    [Tooltip("Level 2 and level 3, in that order; each adds to the one before. " +
             "Empty: the skill has only level 1 and its levels are never offered.")]
    [SerializeField] private SkillLevelModifier[] _levels = new SkillLevelModifier[0];

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public string Description => _description;

    /// <summary>
    /// 1 plus its level modifiers: 3 for the catalog's skills.
    /// </summary>
    public int MaxLevel => 1 + (_levels != null ? _levels.Length : 0);

    /// <summary>
    /// What getting that level (2 or 3) changes. Null for level 1 or one it doesn't have.
    /// </summary>
    public SkillLevelModifier GetLevelModifier(int level)
    {
        int index = level - 2;
        return _levels != null && index >= 0 && index < _levels.Length ? _levels[index] : null;
    }
    public bool CanDetonate => _canDetonate;
    public AuraStrength AuraStrength => _auraStrength;
    public float Cooldown => _cooldown;
    public float CastRange => _castRange;
    public SkillShape Shape => _shape;
    public float Radius => _radius;
    public float Length => _length;
    public float Width => _width;
    public float Angle => _angle;
    public float ProjectileSpeed => _projectileSpeed;
    public float ProjectileRadius => _projectileRadius;
    public float ImpactRadius => _impactRadius;
    public int Pierce => _pierce;
    public bool BurstAtEnd => _burstAtEnd;
    public float TrapDuration => _trapDuration;
    public SkillProjectile ProjectilePrefab => _projectilePrefab;
    public bool ProjectileTrail => _projectileTrail;
    public bool IsProjectile => _shape == SkillShape.Projectile;
    public float Duration => _duration;
    public float HitInterval => _hitInterval;

    /// <summary>
    /// Walls and turrets: placed where you aim, they stay and the host makes them hit.
    /// </summary>
    public bool IsPlaced => _shape == SkillShape.Wall || _shape == SkillShape.Turret;
    public int Damage => _damage;
    public float Knockback => _knockback;
    public float KnockbackDuration => _knockbackDuration;
    public SkillShapeVisual Visual => _visual;

    /// <summary>
    /// Lines, cones and projectiles start at the caster and point at the cursor; circles, walls and turrets
    /// land on it.
    /// </summary>
    public bool IsAimedByDirection => _shape == SkillShape.Line || _shape == SkillShape.Cone || _shape == SkillShape.Projectile;

    public ElementType GetElement(ElementType casterElement)
    {
        return _usesCasterElement ? casterElement : _element;
    }
}
