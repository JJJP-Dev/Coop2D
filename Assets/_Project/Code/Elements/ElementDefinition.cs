using UnityEngine;

[CreateAssetMenu(fileName = "ElementDefinition", menuName = "Coop2D/Elements/Element Definition")]
public class ElementDefinition : ScriptableObject
{
    [SerializeField] private ElementType _type;

    [Tooltip("Shown to players.")]
    [SerializeField] private string _displayName;

    [Tooltip("Placeholder until the shared palette is frozen (#44).")]
    [SerializeField] private Color _color = Color.white;

    public ElementType Type => _type;
    public string DisplayName => _displayName;
    public Color Color => _color;
}
