using System.Collections.Generic;

// What the host remembers about an accepted projectile skill (#162), to check what its caster reports.
// A class and not a struct: the hits left and the enemies already hit change with each report.
public class SkillShotRecord
{
    public AttackSlot Slot { get; }
    public SkillDefinition Skill { get; }

    // Its path, as wide as the projectile, from where the caster stood.
    public SkillArea Path { get; }

    // In the host's real time, like the cooldowns.
    public double ExpiresAt { get; }

    // Enemies it can still hit directly (Rayo: 2). One that bursts reports once and is forgotten.
    public int HitsLeft { get; set; }

    public HashSet<Health> Hit { get; } = new();

    public SkillShotRecord(AttackSlot slot, SkillDefinition skill, SkillArea path, double expiresAt, int hits)
    {
        Slot = slot;
        Skill = skill;
        Path = path;
        ExpiresAt = expiresAt;
        HitsLeft = hits;
    }
}
