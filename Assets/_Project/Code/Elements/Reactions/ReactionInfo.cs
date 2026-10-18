using FishNet.Object;
using UnityEngine;

// A reaction the host triggered: which one, where, and who took part.
public readonly struct ReactionInfo
{
    public AuraHolder Target { get; }
    public ReactionDefinition Reaction { get; }

    // Where the host saw the target. Each client shows it interpolated, slightly elsewhere.
    public Vector2 Position { get; }

    // The player who put the aura there. Null if that player left.
    public NetworkObject AuraOwner { get; }

    // The player whose skill detonated it.
    public NetworkObject Detonator { get; }

    public ReactionInfo(AuraHolder target, ReactionDefinition reaction, Vector2 position,
                        NetworkObject auraOwner, NetworkObject detonator)
    {
        Target = target;
        Reaction = reaction;
        Position = position;
        AuraOwner = auraOwner;
        Detonator = detonator;
    }

    // Two different players: the "¡DÚO!" of the design document.
    public bool IsDuo => AuraOwner != null && Detonator != null && AuraOwner != Detonator;

    // True on the clients of both participants, who hear it louder.
    public bool InvolvesLocalPlayer => (AuraOwner != null && AuraOwner.IsOwner) || (Detonator != null && Detonator.IsOwner);
}
