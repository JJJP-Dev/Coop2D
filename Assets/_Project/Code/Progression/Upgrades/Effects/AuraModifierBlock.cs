using System;
using UnityEngine;

// Changes the auras this player applies for the rest of the match (catalog AuraModifier):
//   Auras tenaces: strong auras last +1.5 s.
//   Aura expansiva: a skill also leaves its aura up to 1 m around its circle, on up to 8 more enemies.
// They're stats like any other, so stacks add up and the cap (an aura at most doubles its duration)
// is in PlayerStatSettings. Only the host uses them: auras are always the host's decision.
[Serializable]
public class AuraModifierBlock : EffectBlock
{
    [Tooltip("Which auras last longer.")]
    [SerializeField] private AuraStrength _strength = AuraStrength.Strong;

    [Tooltip("Seconds added to those auras. 0: no change.")]
    [SerializeField] private float _durationAdd;

    [Tooltip("Metres around a skill's circle where its aura also lands, without damage. 0: no change.")]
    [SerializeField, Min(0f)] private float _skillExtraRadius;

    [Tooltip("Most enemies in that ring that get the aura on each cast.")]
    [SerializeField, Min(0)] private int _skillExtraTargets;

    public AuraStrength Strength => _strength;
    public float DurationAdd => _durationAdd;
    public float SkillExtraRadius => _skillExtraRadius;
    public int SkillExtraTargets => _skillExtraTargets;

    public override void Acquire(EffectContext context)
    {
        if (context.Stats == null)
            return;

        if (_durationAdd != 0f)
        {
            StatId stat = _strength == AuraStrength.Strong ? StatId.StrongAuraDuration : StatId.WeakAuraDuration;
            context.Stats.AddBonus(new StatBonus(stat, StatUnit.Flat, _durationAdd, context.Source));
        }

        if (_skillExtraRadius > 0f)
            context.Stats.AddBonus(new StatBonus(StatId.SkillAuraExtraRadius, StatUnit.Flat, _skillExtraRadius, context.Source));

        if (_skillExtraTargets > 0)
            context.Stats.AddBonus(new StatBonus(StatId.SkillAuraExtraTargets, StatUnit.Flat, _skillExtraTargets, context.Source));
    }
}
