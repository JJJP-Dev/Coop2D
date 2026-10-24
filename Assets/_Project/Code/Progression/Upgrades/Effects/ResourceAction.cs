using System;
using UnityEngine;

// Gives the player something of the match (catalog Resource): for now, thirds of a reroll, within
// the limit of 2 per match. Respiro gives one third.
[Serializable]
public class ResourceAction : EffectAction
{
    [Tooltip("Thirds of a reroll. 3 is a whole one.")]
    [SerializeField, Min(0)] private int _rerollThirds;

    public int RerollThirds => _rerollThirds;

    public override void Run(EffectContext context)
    {
        if (_rerollThirds > 0 && context.Offers != null)
            context.Player.EarnRerollThirds(_rerollThirds, context.Offers.MaxRerollThirds);
    }
}
