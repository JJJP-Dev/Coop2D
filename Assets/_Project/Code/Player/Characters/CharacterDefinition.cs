using UnityEngine;

// A character as data (#162): who you play decides your element and your skills.
// The fields follow the progression catalog (E.4): Brasa, Marea and Voltio in the Pre 3, Filo later.
[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Coop2D/Player/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: ember, tide, volt. Upgrade requirements (char:ember) " +
             "and the launcher's events use it, so once a build is out it never changes.")]
    [SerializeField] private string _id;

    [Tooltip("Shown to players: Brasa, Marea, Voltio.")]
    [SerializeField] private string _displayName;

    [Tooltip("The character's main element: the one of its basic attack and of the skills that take the caster's element.")]
    [SerializeField] private ElementType _element;

    [Tooltip("Slot 1 (left click), slot 2 (right click) and slot 3 (Q). Empty means no skill there. " +
             "Until the kits arrive (#162), the test skills Estallido and Marca.")]
    [SerializeField] private SkillDefinition[] _startingSkills = new SkillDefinition[PlayerSkills.SlotCount];

    public string Id => _id;
    public string DisplayName => _displayName;
    public ElementType Element => _element;

    /// <summary>
    /// The skill in that slot index (0 to 2) at the start of a match, or null.
    /// </summary>
    public SkillDefinition GetStartingSkill(int index)
    {
        return index >= 0 && index < _startingSkills.Length ? _startingSkills[index] : null;
    }

    private void OnValidate()
    {
        if (_startingSkills.Length != PlayerSkills.SlotCount)
            Debug.LogWarning($"[CharacterDefinition] {name}: expected {PlayerSkills.SlotCount} skill slots, found {_startingSkills.Length}.", this);

        if (_element == ElementType.None)
            Debug.LogWarning($"[CharacterDefinition] {name} has no element.", this);
    }
}
