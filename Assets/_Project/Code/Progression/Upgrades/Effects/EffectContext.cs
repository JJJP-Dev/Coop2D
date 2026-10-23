using FishNet.Object;

// Who an effect block works on (#161): the player who has the upgrade, their parts and the upgrade
// itself. The host makes one each time it applies an upgrade or an event happens, so the blocks don't
// have to look anything up. The owner makes one too when it predicts its own skill cast (#157).
public class EffectContext
{
    public EffectContext(PlayerUpgrades player, PlayerStats stats, Health health, UpgradeDefinition upgrade,
                         OfferSettings offers, uint tick = 0, NetworkObject partner = null)
    {
        Player = player;
        Stats = stats;
        Health = health;
        Upgrade = upgrade;
        Offers = offers;
        Tick = tick;
        Partner = partner;
    }

    public PlayerUpgrades Player { get; }

    /// <summary>
    /// Null on a player without stats.
    /// </summary>
    public PlayerStats Stats { get; }

    /// <summary>
    /// Null on a player without health.
    /// </summary>
    public Health Health { get; }

    public UpgradeDefinition Upgrade { get; }

    /// <summary>
    /// For the rerolls' limit. Null while predicting.
    /// </summary>
    public OfferSettings Offers { get; }

    /// <summary>
    /// The tick of the event, in the host's clock: timed effects start on it. 0 when the upgrade is taken.
    /// </summary>
    public uint Tick { get; }

    /// <summary>
    /// The other player of a duo reaction. Null otherwise.
    /// </summary>
    public NetworkObject Partner { get; }

    /// <summary>
    /// The upgrade's id, to say where a bonus comes from.
    /// </summary>
    public string Source => Upgrade != null ? Upgrade.Id : "";
}
