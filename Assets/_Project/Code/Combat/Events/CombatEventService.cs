using System;
using UnityEngine;

// Hits, deaths and reactions confirmed by the host, for presentation only: nothing here changes the game.
// Each client raises every event once, the host's client included. Only Health and AuraHolder raise them.
public class CombatEventService : MonoBehaviour
{
    public event Action<HitInfo> HitConfirmed;

    // Raised right after the HitConfirmed of the killing blow, with the same HitInfo.
    public event Action<HitInfo> DeathConfirmed;

    // A skill applies its aura before its damage, so its reactions arrive before its hits.
    public event Action<ReactionInfo> ReactionTriggered;

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
}
