using UnityEngine;

// A skill as one player has it (#162): its data and its level, with the numbers of that level already
// worked out. Every PC builds the same one from the id and level PlayerSkills syncs, so the host's checks
// and every client's picture use the same numbers. Immutable: a new level is a new instance.
// It reads like a SkillDefinition, so the code that casts skills only asks for the numbers.
public sealed class SkillInstance
{
    public SkillInstance(SkillDefinition definition, int level)
    {
        Definition = definition;
        Level = Mathf.Clamp(level, 1, definition.MaxLevel);

        float damagePercent = 0f;
        float cooldownPercent = 0f;
        float radiusAdd = 0f;
        float lengthAdd = 0f;
        float angleAdd = 0f;
        float impactAdd = 0f;
        float trapAdd = 0f;
        int pierceAdd = 0;

        // Level 2 is the first modifier, level 3 the second; they add up (catalog E.0 rule 5).
        for (int i = 0; i < Level - 1; i++)
        {
            SkillLevelModifier modifier = definition.GetLevelModifier(i + 2);
            if (modifier == null)
                continue;

            damagePercent += modifier.DamagePercent;
            cooldownPercent += modifier.CooldownPercent;
            radiusAdd += modifier.RadiusAdd;
            lengthAdd += modifier.LengthAdd;
            angleAdd += modifier.AngleAdd;
            impactAdd += modifier.ImpactRadiusAdd;
            trapAdd += modifier.TrapDurationAdd;
            pierceAdd += modifier.PierceAdd;
        }

        Damage = Mathf.Max(0, Mathf.RoundToInt(definition.Damage * (1f + damagePercent / 100f)));
        Cooldown = Mathf.Max(0.1f, definition.Cooldown * (1f + cooldownPercent / 100f));
        Radius = Mathf.Max(0.1f, definition.Radius + radiusAdd);
        Length = Mathf.Max(0.1f, definition.Length + lengthAdd);
        Angle = Mathf.Clamp(definition.Angle + angleAdd, 10f, 180f);
        ImpactRadius = Mathf.Max(0f, definition.ImpactRadius + impactAdd);
        TrapDuration = Mathf.Max(0f, definition.TrapDuration + trapAdd);
        Pierce = Mathf.Max(0, definition.Pierce + pierceAdd);
    }

    public SkillDefinition Definition { get; }

    /// <summary>
    /// From 1 to the skill's MaxLevel.
    /// </summary>
    public int Level { get; }

    // The numbers that levels can change.
    public int Damage { get; }
    public float Cooldown { get; }
    public float Radius { get; }
    public float Length { get; }
    public float Angle { get; }
    public float ImpactRadius { get; }
    public float TrapDuration { get; }
    public int Pierce { get; }

    // Projectiles fly at their base speed, so how far they go sets how long (Chispa's +2 m is a line).
    public float FlightTime => Length / Definition.ProjectileSpeed;

    // The rest, as the data says.
    public string Id => Definition.Id;
    public string DisplayName => Definition.DisplayName;
    public Sprite Icon => Definition.Icon;
    public bool CanDetonate => Definition.CanDetonate;
    public AuraStrength AuraStrength => Definition.AuraStrength;
    public float CastRange => Definition.CastRange;
    public SkillShape Shape => Definition.Shape;
    public float Width => Definition.Width;
    public float ProjectileSpeed => Definition.ProjectileSpeed;
    public float ProjectileRadius => Definition.ProjectileRadius;
    public bool BurstAtEnd => Definition.BurstAtEnd;
    public SkillProjectile ProjectilePrefab => Definition.ProjectilePrefab;
    public float Knockback => Definition.Knockback;
    public float KnockbackDuration => Definition.KnockbackDuration;
    public SkillShapeVisual Visual => Definition.Visual;
    public bool IsAimedByDirection => Definition.IsAimedByDirection;
    public bool IsProjectile => Definition.IsProjectile;
    public bool Bursts => ImpactRadius > 0f;

    public ElementType GetElement(ElementType casterElement)
    {
        return Definition.GetElement(casterElement);
    }
}
