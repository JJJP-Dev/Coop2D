// A wall or a turret on the ground, as the host keeps it while it lasts. A class and not a struct: its
// next hit changes every time it hits.
public class SkillPlacement
{
    public AttackSlot Slot { get; }

    // With the level it had when it was placed, even if the skill levels up while it stands.
    public SkillInstance Skill { get; }

    // The wall's band, or the circle a turret reaches.
    public SkillArea Area { get; }

    // In the host's Time.time, like auras: game time.
    public float EndsAt { get; }
    public float NextHitAt { get; set; }

    public SkillPlacement(AttackSlot slot, SkillInstance skill, SkillArea area, float now)
    {
        Slot = slot;
        Skill = skill;
        Area = area;
        EndsAt = now + skill.Duration;

        // The first hit is right away, so a wall placed on enemies burns them at once.
        NextHitAt = now;
    }
}
