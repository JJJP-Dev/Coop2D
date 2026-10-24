using UnityEngine;

// One row of the reaction table: an ordered pair (aura, detonator) and the variant it triggers.
// Both orders of a pair share the reaction and its name; the aura's element picks the variant.
[CreateAssetMenu(fileName = "ReactionDefinition", menuName = "Coop2D/Elements/Reaction Definition")]
public class ReactionDefinition : ScriptableObject
{
    [Tooltip("The reaction both orders of a pair share, as the catalog names it: vapor, electrocution, overload.")]
    [SerializeField] private string _id;

    [Tooltip("This row's variant, unique: the reaction, the aura's element and the detonator's. vapor_fire_water...")]
    [SerializeField] private string _variantId;

    [Tooltip("Shown to players. Both variants of a reaction use the same name.")]
    [SerializeField] private string _displayName;

    [Tooltip("The aura already on the enemy. It picks the variant.")]
    [SerializeField] private ElementType _auraElement;

    [Tooltip("The element of the skill that detonates it.")]
    [SerializeField] private ElementType _detonatorElement;

    [Header("Damage")]
    [Tooltip("Dealt once, when it triggers, to every enemy within Damage Radius of the target. " +
             "It belongs to the reaction, not to the skill that detonated it.")]
    [SerializeField, Min(0)] private int _damage;

    [Tooltip("In units. 0 hits only the target.")]
    [SerializeField, Min(0f)] private float _damageRadius;

    [Header("Cloud (Vapor)")]
    [Tooltip("Seconds. 0 leaves no cloud.")]
    [SerializeField, Min(0f)] private float _cloudDuration;

    [SerializeField, Min(0f)] private float _cloudRadius;

    [Tooltip("How much slower enemies move while inside, from 0 to 60 %.")]
    [SerializeField, Range(0f, 60f)] private float _cloudSlowPercent;

    [Header("Chain (Electrocution)")]
    [Tooltip("Jumps from the target to the nearest enemy with an aura of Aura Element, then from that one, " +
             "each enemy once. They don't consume those auras. 0 has no chain.")]
    [SerializeField, Min(0)] private int _chainJumps;

    [SerializeField, Min(0)] private int _chainDamage;

    [Tooltip("In units, from the last enemy hit.")]
    [SerializeField, Min(0f)] private float _chainRange;

    [Header("Push and stun (Overload)")]
    [Tooltip("In units, straight out from the target, for every enemy within Damage Radius. 0 doesn't push.")]
    [SerializeField, Min(0f)] private float _knockbackDistance;

    [Tooltip("Seconds the push takes. Short on purpose, so it doesn't scatter enemies out of a teammate's reaction.")]
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f;

    [Tooltip("Seconds every enemy within Damage Radius stays still. 0 doesn't stun.")]
    [SerializeField, Min(0f)] private float _stunDuration;

    [Header("Look")]
    [Tooltip("Color of its burst, cloud and chain. Placeholder until the shared palette is frozen.")]
    [SerializeField] private Color _color = Color.white;

    [Tooltip("The headline's gradient, from the top of the letters to the bottom.")]
    [SerializeField] private Color _headlineTopColor = Color.white;

    [SerializeField] private Color _headlineBottomColor = Color.white;

    public string Id => _id;
    public string VariantId => _variantId;
    public string DisplayName => _displayName;
    public ElementType AuraElement => _auraElement;
    public ElementType DetonatorElement => _detonatorElement;
    public int Damage => _damage;
    public float DamageRadius => _damageRadius;
    public float CloudDuration => _cloudDuration;
    public float CloudRadius => _cloudRadius;
    public float CloudSlowPercent => _cloudSlowPercent;
    public int ChainJumps => _chainJumps;
    public int ChainDamage => _chainDamage;
    public float ChainRange => _chainRange;
    public float KnockbackDistance => _knockbackDistance;
    public float KnockbackDuration => _knockbackDuration;
    public float StunDuration => _stunDuration;
    public Color Color => _color;
    public Color HeadlineTopColor => _headlineTopColor;
    public Color HeadlineBottomColor => _headlineBottomColor;
}
