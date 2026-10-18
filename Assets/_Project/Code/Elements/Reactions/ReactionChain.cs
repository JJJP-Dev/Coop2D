using System.Collections.Generic;
using UnityEngine;

// A chain to draw on this client: the points it jumps through, in order, and its color.
public readonly struct ReactionChain
{
    public IReadOnlyList<Vector2> Points { get; }
    public Color Color { get; }

    public ReactionChain(IReadOnlyList<Vector2> points, Color color)
    {
        Points = points;
        Color = color;
    }
}
