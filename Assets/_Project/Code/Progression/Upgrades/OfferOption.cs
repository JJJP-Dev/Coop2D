// One option of an offer: an upgrade, a new skill or the next level of one of the player's skills,
// and the place it fills.
public readonly struct OfferOption
{
    // How a skill's option travels and is remembered (seen, banished): «skill:ember_flare» for the new
    // skill, «skill:ember_flare@2» for its level 2. An upgrade's key is its id.
    public const string SkillPrefix = "skill:";

    public readonly UpgradeDefinition Upgrade;
    public readonly SkillDefinition Skill;

    // 1 for a new skill; 2 or 3 for that level of a skill the player has. 0 for an upgrade.
    public readonly int SkillLevel;

    public readonly OfferPlace Place;

    public OfferOption(UpgradeDefinition upgrade, OfferPlace place)
    {
        Upgrade = upgrade;
        Skill = null;
        SkillLevel = 0;
        Place = place;
    }

    public OfferOption(SkillDefinition skill, int level, OfferPlace place)
    {
        Upgrade = null;
        Skill = skill;
        SkillLevel = level;
        Place = place;
    }

    public bool IsSkill => Skill != null;

    /// <summary>
    /// The same option in another place: a guaranteed skill that ends up in an open place keeps its draw.
    /// </summary>
    public OfferOption WithPlace(OfferPlace place)
    {
        return IsSkill ? new OfferOption(Skill, SkillLevel, place) : new OfferOption(Upgrade, place);
    }

    /// <summary>
    /// What identifies it: in the offers sent to the owner, what was seen and what was banished.
    /// </summary>
    public string Key => IsSkill ? SkillKey(Skill.Id, SkillLevel) : Upgrade != null ? Upgrade.Id : "";

    /// <summary>
    /// An evolution stays through rerolls and can't be banished.
    /// </summary>
    public bool IsLocked => Place == OfferPlace.Evolution;

    public bool CanBanish => Place != OfferPlace.Evolution && Place != OfferPlace.Fallback;

    public static string SkillKey(string skillId, int level)
    {
        return level <= 1 ? SkillPrefix + skillId : $"{SkillPrefix}{skillId}@{level}";
    }

    /// <summary>
    /// Reads a skill key back. False for an upgrade's key.
    /// </summary>
    public static bool TryParseSkillKey(string key, out string skillId, out int level)
    {
        skillId = null;
        level = 0;
        if (string.IsNullOrEmpty(key) || !key.StartsWith(SkillPrefix))
            return false;

        string rest = key.Substring(SkillPrefix.Length);
        int at = rest.IndexOf('@');
        skillId = at < 0 ? rest : rest.Substring(0, at);
        level = at < 0 ? 1 : int.TryParse(rest.Substring(at + 1), out int parsed) ? parsed : 0;
        return !string.IsNullOrEmpty(skillId) && level >= 1;
    }
}
