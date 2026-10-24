using System;
using UnityEngine;

// What one level of a skill changes: level 2 and level 3 are each one of
// these, and they add up. Percentages are of the skill's base numbers; the rest add to them.
[Serializable]
public class SkillLevelModifier
{
    [Tooltip("One line for the offer: «+20 % de daño (36)».")]
    [SerializeField] private string _description;

    [Tooltip("Percent of the base damage: 20 is +20 %.")]
    [SerializeField] private float _damagePercent;

    [Tooltip("Percent of the base cooldown: −15 is 15 % shorter.")]
    [SerializeField] private float _cooldownPercent;

    [Tooltip("Units added to a circle's radius.")]
    [SerializeField] private float _radiusAdd;

    [Tooltip("Units added to a line's or a cone's length, or to how far a projectile flies.")]
    [SerializeField] private float _lengthAdd;

    [Tooltip("Degrees added to a cone's opening.")]
    [SerializeField] private float _angleAdd;

    [Tooltip("Units added to the circle a projectile bursts in.")]
    [SerializeField] private float _impactRadiusAdd;

    [Tooltip("Seconds added to how long a projectile traps the enemy it touched.")]
    [SerializeField] private float _trapDurationAdd;

    [Tooltip("Enemies more a direct projectile goes through.")]
    [SerializeField] private int _pierceAdd;

    public string Description => _description;
    public float DamagePercent => _damagePercent;
    public float CooldownPercent => _cooldownPercent;
    public float RadiusAdd => _radiusAdd;
    public float LengthAdd => _lengthAdd;
    public float AngleAdd => _angleAdd;
    public float ImpactRadiusAdd => _impactRadiusAdd;
    public float TrapDurationAdd => _trapDurationAdd;
    public int PierceAdd => _pierceAdd;
}
