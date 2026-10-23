// How rare an upgrade is (#78, catalog C.2). Assets store the number, so never renumber one.
// Common, Rare and Epic come from the rarity roll; Legendary is in the data with weight 0 until the 0.2.
// Duo and Evolution never come from that roll: they have their own rules (C.4 and C.6).
public enum UpgradeRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
    Duo = 4,
    Evolution = 5,
}
