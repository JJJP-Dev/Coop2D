using System;
using System.Collections.Generic;
using UnityEngine;

// Stat changes that only count on hits whose target meets a condition (catalog Conditional):
// Cazador de auras is +15 % of skill damage against enemies with an aura. It holds stat changes only.
// The host checks the condition on each hit, before the hit changes the target: a skill that detonates
// the aura still gets the bonus.
[Serializable]
public class ConditionalBlock : EffectBlock
{
    [Tooltip("What the target must have. With several, it needs them all.")]
    [SerializeField] private TargetConditions _conditions = TargetConditions.HasAura;

    [Tooltip("The changes that count when it does. Only skill damage reads conditions today.")]
    [SerializeField] private List<StatModifierBlock> _modifiers = new();

    public TargetConditions Conditions => _conditions;
    public IReadOnlyList<StatModifierBlock> Modifiers => _modifiers;

    public override void Acquire(EffectContext context)
    {
        if (context.Stats == null)
            return;

        foreach (StatModifierBlock modifier in _modifiers)
        {
            if (modifier != null)
                context.Stats.AddConditionalBonus(_conditions, new StatBonus(modifier.Stat, modifier.Unit, modifier.Value, context.Source));
        }
    }
}
