using System.Collections.Generic;
using UnityEngine;

// A character as data: who you play decides your element and your skills.
// The fields follow the progression catalog's data: Brasa, Marea and Voltio for now, Filo later.
[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Coop2D/Player/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique, as in the catalog: ember, tide, volt. Upgrade requirements (char:ember) " +
             "and the launcher's events use it, so once a build is out it never changes.")]
    [SerializeField] private string _id;

    [Tooltip("Shown to players: Brasa, Marea, Voltio.")]
    [SerializeField] private string _displayName;

    [Tooltip("How the character looks: its idle sprite, until it has animations. Empty keeps the sprite of " +
             "the player prefab.")]
    [SerializeField] private Sprite _sprite;

    [Tooltip("The character's main element: the one of its basic attack and of the skills that take the caster's element.")]
    [SerializeField] private ElementType _element;

    [Tooltip("The skill every match starts with, in slot 1: Llamarada, Ola, Rayo.")]
    [SerializeField] private SkillDefinition _startingSkill;

    [Tooltip("The rest of the character's own skills, which the offers can give. The " +
             "starting one doesn't go here. They weigh more than the generic ones in the offers.")]
    [SerializeField] private List<SkillDefinition> _ownSkills = new();

    public string Id => _id;
    public string DisplayName => _displayName;
    public ElementType Element => _element;
    public Sprite Sprite => _sprite;

    public SkillDefinition StartingSkill => _startingSkill;
    public IReadOnlyList<SkillDefinition> OwnSkills => _ownSkills;

    private void OnValidate()
    {
        if (_startingSkill == null)
            Debug.LogWarning($"[CharacterDefinition] {name} has no starting skill.", this);

        if (_startingSkill != null && _ownSkills.Contains(_startingSkill))
            Debug.LogWarning($"[CharacterDefinition] {name}: the starting skill shouldn't be in Own Skills too.", this);

        if (_element == ElementType.None)
            Debug.LogWarning($"[CharacterDefinition] {name} has no element.", this);
    }
}
