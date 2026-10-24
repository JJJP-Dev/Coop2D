using UnityEngine;

// One experience orb on the ground, as it travels inside ExperienceOrbs' list.
// A plain struct with public fields, so FishNet writes it without a serializer of our own.
public struct ExperienceOrb
{
    // Unique while the match lasts, so a client can tell which orb was picked up even after the list moved.
    public ushort Id;
    public Vector2 Position;
    public int Value;

    public ExperienceOrb(ushort id, Vector2 position, int value)
    {
        Id = id;
        Position = position;
        Value = value;
    }
}
