using System.Collections.Generic;
using UnityEngine;

// Every skill in the game. A player's skills travel over the network as ids, and each PC finds
// their data here, so a skill missing from Skills can't be had. The generic ones can be offered to any
// character; a character's own ones are in its CharacterDefinition.
[CreateAssetMenu(fileName = "SkillLibrary", menuName = "Coop2D/Skills/Skill Library")]
public class SkillLibrary : ScriptableObject
{
    [Tooltip("Every skill, the test ones included. The order is the order F1's «Dar habilidad» goes through.")]
    [SerializeField] private List<SkillDefinition> _skills = new();

    [Tooltip("Skills any character can be offered. Can be empty: then only the character's own are offered.")]
    [SerializeField] private List<SkillDefinition> _generic = new();

    public IReadOnlyList<SkillDefinition> Skills => _skills;
    public IReadOnlyList<SkillDefinition> Generic => _generic;

    /// <summary>
    /// Null for an unknown id, or a null or empty one.
    /// </summary>
    public SkillDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        foreach (SkillDefinition skill in _skills)
        {
            if (skill != null && skill.Id == id)
                return skill;
        }

        return null;
    }

    private void OnValidate()
    {
        // A generic skill has to be findable by its id too.
        foreach (SkillDefinition skill in _generic)
        {
            if (skill != null && !_skills.Contains(skill))
                Debug.LogWarning($"[SkillLibrary] {skill.name} is generic but isn't in Skills.", this);
        }
    }
}
