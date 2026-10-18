using System;
using UnityEngine;

// Hits, deaths and reactions confirmed by the host, for presentation only: nothing here changes the game.
// Each client raises every event once, the host's client included. Only Health, AuraHolder and ReactionService raise them.
public class CombatEventService : MonoBehaviour
{
    public event Action<HitInfo> HitConfirmed;

    // Raised right after the HitConfirmed of the killing blow, with the same HitInfo.
    public event Action<HitInfo> DeathConfirmed;

    // A skill applies its aura before its damage, so its reactions arrive before its hits.
    public event Action<ReactionInfo> ReactionTriggered;

    // Right after the ReactionTriggered of an Electrocution that jumped.
    public event Action<ReactionChain> ChainTriggered;

    public void RaiseHit(HitInfo hit)
    {
        HitConfirmed?.Invoke(hit);
    }

    public void RaiseDeath(HitInfo hit)
    {
        DeathConfirmed?.Invoke(hit);
    }

    public void RaiseReaction(ReactionInfo reaction)
    {
        ReactionTriggered?.Invoke(reaction);
    }

    public void RaiseChain(ReactionChain chain)
    {
        ChainTriggered?.Invoke(chain);
    }
}
