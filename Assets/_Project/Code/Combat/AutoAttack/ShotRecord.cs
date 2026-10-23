using System.Collections.Generic;
using UnityEngine;

// What the host remembers about an accepted shot, to check the hit its shooter reports.
public readonly struct ShotRecord
{
    // The line the projectile flies along, up to where it disappears.
    public Vector2 Start { get; }
    public Vector2 End { get; }

    // In the host's Time.time.
    public float ExpiresAt { get; }

    // The enemies already hit by the projectiles of the same volley (Doble disparo, #161), shared by all
    // of them. Null for a single projectile.
    public HashSet<Health> VolleyHits { get; }

    public ShotRecord(Vector2 start, Vector2 end, float expiresAt, HashSet<Health> volleyHits)
    {
        Start = start;
        End = end;
        ExpiresAt = expiresAt;
        VolleyHits = volleyHits;
    }
}
