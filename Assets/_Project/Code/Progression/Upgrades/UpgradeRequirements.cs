using System.Collections.Generic;
using UnityEngine;

// Whether a player meets the requirements of an upgrade, read from the same text the catalog uses:
//   kind:value               basic:projectile, char:ember, skill:ember_flare@2, element:fire...
//   the id of an upgrade     the player must have taken it at least once.
// Plain C#: PlayerUpgrades builds one with the player's parts, and the offer generator asks through it.
public class UpgradeRequirements
{
    // Requirement kinds this build doesn't know, each warned about once for the whole game.
    private static readonly HashSet<string> WarnedKinds = new();

    private readonly PlayerUpgrades _upgrades;
    private readonly PlayerCharacter _character;
    private readonly PlayerElement _element;
    private readonly PlayerSkillSlots _skills;
    private readonly bool _hasProjectileBasic;

    /// <param name="character">Optional, as element and skills: without them fewer requirements hold.</param>
    public UpgradeRequirements(PlayerUpgrades upgrades, PlayerCharacter character, PlayerElement element,
                               PlayerSkillSlots skills, bool hasProjectileBasic)
    {
        _upgrades = upgrades;
        _character = character;
        _element = element;
        _skills = skills;
        _hasProjectileBasic = hasProjectileBasic;
    }

    public bool Meets(string requirement)
    {
        if (string.IsNullOrEmpty(requirement))
            return true;

        int colon = requirement.IndexOf(':');
        if (colon < 0)
            return _upgrades.GetStacks(requirement) > 0;

        string kind = requirement.Substring(0, colon);
        string value = requirement.Substring(colon + 1);

        switch (kind)
        {
            case "basic":
                return value == "projectile" && _hasProjectileBasic;

            case "char":
                return _character != null && _character.Id == value;

            case "skill":
                return HasSkill(value);

            // The basic attack and the skills that take the caster's element all use the player's
            // element: the character's, or the one a cheat set.
            case "element":
            case "basicElement":
                return TryParseElement(value, out ElementType element) && PlayerElementIs(element);

            case "skillElement":
                return TryParseElement(value, out ElementType skillElement) && HasSkillOfElement(skillElement);

            default:
                // Kinds of systems that don't exist yet. Never holding is the safe side: an upgrade is
                // never offered without what it needs.
                if (WarnedKinds.Add(kind))
                    Debug.LogWarning($"[UpgradeRequirements] Requirements of kind «{kind}» aren't known yet: {requirement} never holds.", _upgrades);

                return false;
        }
    }

    // skill:id, or skill:id@level: the player has it at that level or more.
    private bool HasSkill(string value)
    {
        int at = value.IndexOf('@');
        string id = at < 0 ? value : value.Substring(0, at);
        int level = at < 0 || !int.TryParse(value.Substring(at + 1), out int parsed) ? 1 : parsed;

        return _skills != null && _skills.GetLevel(id) >= level;
    }

    private bool HasSkillOfElement(ElementType element)
    {
        if (_skills == null || _element == null)
            return false;

        for (int slot = 0; slot < _skills.Count; slot++)
        {
            SkillInstance skill = _skills.Get(slot);
            if (skill != null && skill.GetElement(_element.Current) == element)
                return true;
        }

        return false;
    }

    private bool PlayerElementIs(ElementType element)
    {
        return _element != null && _element.Current == element;
    }

    private static bool TryParseElement(string value, out ElementType element)
    {
        element = value switch
        {
            "fire" => ElementType.Fire,
            "water" => ElementType.Water,
            "electric" => ElementType.Electric,
            _ => ElementType.None,
        };

        return element != ElementType.None;
    }
}
