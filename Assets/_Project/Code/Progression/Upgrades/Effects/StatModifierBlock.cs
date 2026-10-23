using System;
using UnityEngine;

// Changes one of the player's stats for the rest of the match (#161, catalog StatModifier): Cañón largo
// gives +3 of basic range, Imán +50 % of pickup radius. Each stack adds it again. PlayerStats sums
// them and applies the caps.
[Serializable]
public class StatModifierBlock : EffectBlock
{
    [SerializeField] private StatId _stat;

    [Tooltip("Flat adds to the base value; Percent multiplies the result. Interval, cooldown and damage taken " +
             "go down with negative values.")]
    [SerializeField] private StatUnit _unit;

    [Tooltip("+3, −30... In units, seconds or health for Flat; in % for Percent.")]
    [SerializeField] private float _value;

    public StatId Stat => _stat;
    public StatUnit Unit => _unit;
    public float Value => _value;

    public override void Acquire(EffectContext context)
    {
        if (context.Stats != null)
            context.Stats.AddBonus(new StatBonus(_stat, _unit, _value, context.Source));
    }
}
