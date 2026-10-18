using FishNet.Object;
using UnityEngine;

// A hit the host accepted, as each client receives it.
public readonly struct HitInfo
{
    public Health Target { get; }

    // Null when no one caused it, like the host's test keys.
    public NetworkObject Attacker { get; }

    // Where the host saw the target. Each client shows it interpolated, slightly elsewhere.
    public Vector2 Position { get; }

    public int Amount { get; }
    public DamageKind Kind { get; }

    // The attack's element. None for reactions and test keys.
    public ElementType Element { get; }

    public HitInfo(Health target, NetworkObject attacker, Vector2 position, int amount, DamageKind kind, ElementType element)
    {
        Target = target;
        Attacker = attacker;
        Position = position;
        Amount = amount;
        Kind = kind;
        Element = element;
    }

    // True only on the client whose player caused the hit.
    public bool ByLocalPlayer => Attacker != null && Attacker.IsOwner;
}
