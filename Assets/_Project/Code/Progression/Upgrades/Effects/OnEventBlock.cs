using System;
using System.Collections.Generic;
using UnityEngine;

// Runs its actions when something happens (#161, catalog OnEvent). For now the only event is taking the
// upgrade; the ones that happen during the match (casting a skill, a reaction) come with the timed
// effects (#157).
[Serializable]
public class OnEventBlock : EffectBlock
{
    [SerializeField] private EffectEvent _event;

    [Tooltip("In order. Pick each one's kind with its button.")]
    [SerializeReference, SubclassPicker] private List<EffectAction> _actions = new();

    public EffectEvent Event => _event;
    public IReadOnlyList<EffectAction> Actions => _actions;

    public override void Acquire(EffectContext context)
    {
        if (_event == EffectEvent.Acquired)
            Run(context);
    }

    private void Run(EffectContext context)
    {
        foreach (EffectAction action in _actions)
            action?.Run(context);
    }
}
