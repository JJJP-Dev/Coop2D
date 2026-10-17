using UnityEngine;

// What the host remembers about an accepted shot, to check the hit its shooter reports.
public readonly struct ShotRecord
{
    // The line the projectile flies along, up to where it disappears.
    public Vector2 Start { get; }
    public Vector2 End { get; }

    // In the host's Time.time.
    public float ExpiresAt { get; }

    public ShotRecord(Vector2 start, Vector2 end, float expiresAt)
    {
        Start = start;
        End = end;
        ExpiresAt = expiresAt;
    }
}
