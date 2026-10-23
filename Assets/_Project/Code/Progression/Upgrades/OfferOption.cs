// One option of an offer (#78): the upgrade and the place it fills.
public readonly struct OfferOption
{
    public readonly UpgradeDefinition Upgrade;
    public readonly OfferPlace Place;

    public OfferOption(UpgradeDefinition upgrade, OfferPlace place)
    {
        Upgrade = upgrade;
        Place = place;
    }

    /// <summary>
    /// An evolution stays through rerolls and can't be banished (catalog C.4).
    /// </summary>
    public bool IsLocked => Place == OfferPlace.Evolution;

    public bool CanBanish => Place != OfferPlace.Evolution && Place != OfferPlace.Fallback;
}
