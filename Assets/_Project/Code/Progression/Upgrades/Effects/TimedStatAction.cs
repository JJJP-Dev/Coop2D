using System;
using UnityEngine;

// Changes one stat for a few seconds: Tempo is −20 % of basic interval for 2 s, Viento a favor +25 % of
// speed for 2 s. Inside an OnEventBlock.
// The host adds it with the event's tick, so it ends on the same tick on the host and on the owner. For
// the owner's own skill cast, the owner shows it at once and the host's copy replaces it when it arrives.
[Serializable]
public class TimedStatAction : EffectAction
{
    [SerializeField] private StatId _stat;

    [Tooltip("Flat adds to the base value; Percent multiplies the result.")]
    [SerializeField] private StatUnit _unit = StatUnit.Percent;

    [Tooltip("−20, +25... In units, seconds or health for Flat; in % for Percent.")]
    [SerializeField] private float _value;

    [Tooltip("Seconds it lasts.")]
    [SerializeField, Min(0.1f)] private float _duration = 2f;

    public StatId Stat => _stat;
    public StatUnit Unit => _unit;
    public float Value => _value;
    public float Duration => _duration;

    public override void Run(EffectContext context)
    {
        if (context.Stats != null)
            context.Stats.AddTimedBonus(CreateBonus(context), context.Tick, _duration);
    }

    public override void Predict(EffectContext context)
    {
        if (context.Stats != null)
            context.Stats.PredictTimedBonus(CreateBonus(context), context.Tick, _duration);
    }

    private StatBonus CreateBonus(EffectContext context)
    {
        return new StatBonus(_stat, _unit, _value, context.Source);
    }
}
