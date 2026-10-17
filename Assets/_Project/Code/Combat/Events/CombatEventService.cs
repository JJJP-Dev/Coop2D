using System;
using UnityEngine;

// Hits and deaths confirmed by the host, for presentation only: nothing here changes the game.
// Each client raises every event once, the host's client included. Only Health raises them.
public class CombatEventService : MonoBehaviour
{
    public event Action<HitInfo> HitConfirmed;

    // Raised right after the HitConfirmed of the killing blow, with the same HitInfo.
    public event Action<HitInfo> DeathConfirmed;

    public void RaiseHit(HitInfo hit)
    {
        HitConfirmed?.Invoke(hit);
    }

    public void RaiseDeath(HitInfo hit)
    {
        DeathConfirmed?.Invoke(hit);
    }
}
