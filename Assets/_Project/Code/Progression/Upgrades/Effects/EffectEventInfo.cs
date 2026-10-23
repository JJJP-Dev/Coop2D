using FishNet.Object;

// What happened, for the OnEventBlocks listening (#161). The host fills it in, or the owner when it
// predicts its own skill cast (#157).
public readonly struct EffectEventInfo
{
    public EffectEvent Event { get; }

    // The tick it happened on, in the host's clock. Timed effects and internal cooldowns count from it,
    // so the host and the owner end them on the same tick.
    public uint Tick { get; }

    // Reactions only: which part the player had, and whether two different players took part.
    public bool IsAuraOwner { get; }
    public bool IsDetonator { get; }
    public bool IsDuo { get; }

    // Reactions only: the other player of a duo. Null otherwise, or if they left.
    public NetworkObject Partner { get; }

    public EffectEventInfo(EffectEvent effectEvent, uint tick, bool isAuraOwner = false, bool isDetonator = false,
                           bool isDuo = false, NetworkObject partner = null)
    {
        Event = effectEvent;
        Tick = tick;
        IsAuraOwner = isAuraOwner;
        IsDetonator = isDetonator;
        IsDuo = isDuo;
        Partner = partner;
    }
}
