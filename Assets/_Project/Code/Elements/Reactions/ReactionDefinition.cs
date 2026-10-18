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

    public string Id => _id;
    public string VariantId => _variantId;
    public string DisplayName => _displayName;
    public ElementType AuraElement => _auraElement;
    public ElementType DetonatorElement => _detonatorElement;
}
