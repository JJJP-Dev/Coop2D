// Who an effect block works on (#161): the player who has the upgrade, their parts and the upgrade
// itself. The host makes one each time it applies an upgrade, so the blocks don't have to look anything up.
public class EffectContext
{
    public EffectContext(PlayerUpgrades player, PlayerStats stats, Health health, UpgradeDefinition upgrade,
                         OfferSettings offers)
    {
        Player = player;
        Stats = stats;
        Health = health;
        Upgrade = upgrade;
        Offers = offers;
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
    /// For the rerolls' limit.
    /// </summary>
    public OfferSettings Offers { get; }

    /// <summary>
    /// The upgrade's id, to say where a bonus comes from.
    /// </summary>
    public string Source => Upgrade != null ? Upgrade.Id : "";
}
