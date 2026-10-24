using System;
using UnityEngine;

// Gives a player back health or cooldown (catalog Restore). Vitalidad heals 20 when taken, Respiro
// 30; Relevo gives back 1 s of every cooldown to whoever has it, Asistencia to the partner.
// A downed player isn't healed: only a revive gets them up. A cooldown never ends earlier than 60 % of
// its full time after the cast (in SkillCastSettings).
[Serializable]
public class RestoreAction : EffectAction
{
    [Tooltip("Health given back, up to the maximum.")]
    [SerializeField, Min(0)] private int _heal;

    [Tooltip("Seconds taken off every skill's cooldown that is running.")]
    [SerializeField, Min(0f)] private float _cooldownRefund;

    [Tooltip("Self: who has the upgrade. Partner: the other player of a duo reaction.")]
    [SerializeField] private EffectTarget _target = EffectTarget.Self;

    public int Heal => _heal;
    public float CooldownRefund => _cooldownRefund;
    public EffectTarget Target => _target;

    public override void Run(EffectContext context)
    {
        if (_target == EffectTarget.Self)
        {
            if (context.Health != null && _heal > 0)
                context.Health.Heal(_heal);

            if (_cooldownRefund > 0f && context.Player != null && context.Player.TryGetComponent(out PlayerSkills skills))
                skills.RefundCooldowns(_cooldownRefund);

            return;
        }

        // The partner may have left in the meantime.
        if (context.Partner == null)
            return;

        if (_heal > 0 && context.Partner.TryGetComponent(out Health health))
            health.Heal(_heal);

        if (_cooldownRefund > 0f && context.Partner.TryGetComponent(out PlayerSkills partnerSkills))
            partnerSkills.RefundCooldowns(_cooldownRefund);
    }
}
