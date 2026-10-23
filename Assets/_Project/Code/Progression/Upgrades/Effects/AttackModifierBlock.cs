using System;
using UnityEngine;

// Changes how the basic attack fires for the rest of the match (#161, catalog AttackModifier): more
// projectiles per attack and how wide they fan out. Doble disparo gives +1 projectile across 12°.
// They're stats like any other (BasicProjectiles, BasicSpread), so stacks add up and the cap of +3
// projectiles (A.2) is in PlayerStatSettings. The shooter fires the volley and the host checks it (A.5).
// Damage per projectile isn't here: that's a StatModifier on BasicDamage in the same upgrade.
[Serializable]
public class AttackModifierBlock : EffectBlock
{
    [Tooltip("Extra projectiles per attack.")]
    [SerializeField, Min(0)] private int _projectilesAdd = 1;

    [Tooltip("Degrees added to the fan the projectiles spread across, centred on the aim. With 2 and 12°: −6° and +6°.")]
    [SerializeField, Min(0f)] private float _spreadDegrees = 12f;

    public int ProjectilesAdd => _projectilesAdd;
    public float SpreadDegrees => _spreadDegrees;

    public override void Acquire(EffectContext context)
    {
        if (context.Stats == null)
            return;

        if (_projectilesAdd > 0)
            context.Stats.AddBonus(new StatBonus(StatId.BasicProjectiles, StatUnit.Flat, _projectilesAdd, context.Source));

        if (_spreadDegrees > 0f)
            context.Stats.AddBonus(new StatBonus(StatId.BasicSpread, StatUnit.Flat, _spreadDegrees, context.Source));
    }
}
