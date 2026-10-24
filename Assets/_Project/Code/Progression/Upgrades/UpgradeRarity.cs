// How rare an upgrade is. Assets store the number, so never renumber one.
// Common, Rare and Epic come from the rarity roll; Legendary is in the data with weight 0 for now.
// Duo and Evolution never come from that roll: they have their own rules.
public enum UpgradeRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
    Duo = 4,
    Evolution = 5,
}
