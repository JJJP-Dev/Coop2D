using System;
using UnityEngine;

// Gives the player back health (#161, catalog Restore). Vitalidad heals 20 when taken, Respiro 30.
// A downed player isn't healed: only a revive gets them up.
[Serializable]
public class RestoreAction : EffectAction
{
    [Tooltip("Health given back, up to the maximum.")]
    [SerializeField, Min(0)] private int _heal;

    public int Heal => _heal;

    public override void Run(EffectContext context)
    {
        if (context.Health != null && _heal > 0)
            context.Health.Heal(_heal);
    }
}
