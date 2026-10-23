// What the offer generator needs to know about the player it draws for (#78). PlayerUpgrades is the
// real one; being an interface keeps the generator plain C#, so it can be checked without a match.
public interface IUpgradeHolder
{
    int GetStacks(string id);
    bool IsBanished(string id);
    bool WasSeen(string id);
    bool MeetsRequirement(string requirement);

    /// <summary>
    /// rarity_luck. 0 until player stats exist (#157).
    /// </summary>
    int Luck { get; }

    /// <summary>
    /// Offers in a row that could have had a duo and didn't (C.4).
    /// </summary>
    int OffersWithoutDuo { get; }
}
