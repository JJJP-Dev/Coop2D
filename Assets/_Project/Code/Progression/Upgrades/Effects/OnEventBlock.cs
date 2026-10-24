using System;
using System.Collections.Generic;
using UnityEngine;

// Runs its actions when something happens (catalog OnEvent): taking the upgrade, casting a skill or
// a reaction the player takes part in. PlayerUpgradeEvents tells it what happened and keeps, for each
// player, when it can run again.
[Serializable]
public class OnEventBlock : EffectBlock
{
    [SerializeField] private EffectEvent _event;

    [Tooltip("Reaction only. Which part the player must have had.")]
    [SerializeField] private ReactionRole _role = ReactionRole.Any;

    [Tooltip("Reaction only. Only when two different players took part.")]
    [SerializeField] private bool _duoOnly;

    [Tooltip("Seconds before it can run again for this player. 0: every time.")]
    [SerializeField, Min(0f)] private float _cooldown;

    [Tooltip("Reaction only. Seconds before the same partner can receive it again from this player " +
             "(Asistencia). 0: no limit.")]
    [SerializeField, Min(0f)] private float _recipientCooldown;

    [Tooltip("In order. Pick each one's kind with its button.")]
    [SerializeReference, SubclassPicker] private List<EffectAction> _actions = new();

    public EffectEvent Event => _event;
    public ReactionRole Role => _role;
    public bool DuoOnly => _duoOnly;
    public float Cooldown => _cooldown;
    public float RecipientCooldown => _recipientCooldown;
    public IReadOnlyList<EffectAction> Actions => _actions;

    public override void Acquire(EffectContext context)
    {
        if (_event == EffectEvent.Acquired)
            Run(context);
    }

    /// <summary>
    /// Whether this block listens to what happened. Its cooldowns are checked by whoever keeps them.
    /// </summary>
    public bool Accepts(EffectEventInfo info)
    {
        if (info.Event != _event || _event == EffectEvent.Acquired)
            return false;

        if (_event != EffectEvent.Reaction)
            return true;

        if (_duoOnly && !info.IsDuo)
            return false;

        return _role switch
        {
            ReactionRole.Aura => info.IsAuraOwner,
            ReactionRole.Detonator => info.IsDetonator,
            _ => info.IsAuraOwner || info.IsDetonator,
        };
    }

    /// <summary>
    /// Server only. Every action, in order.
    /// </summary>
    public void Run(EffectContext context)
    {
        foreach (EffectAction action in _actions)
            action?.Run(context);
    }

    /// <summary>
    /// Owner only. What each action shows ahead of the host.
    /// </summary>
    public void Predict(EffectContext context)
    {
        foreach (EffectAction action in _actions)
            action?.Predict(context);
    }
}
