using UnityEngine;

// A Vapor cloud on the host: where it is and how much it slows, until it ends.
public readonly struct ReactionCloud
{
    public Vector2 Center { get; }
    public float Radius { get; }

    // From 0 to 1.
    public float Slow { get; }

    // In the host's Time.time.
    public float EndsAt { get; }

    public ReactionCloud(Vector2 center, float radius, float slow, float endsAt)
    {
        Center = center;
        Radius = radius;
        Slow = slow;
        EndsAt = endsAt;
    }
}
