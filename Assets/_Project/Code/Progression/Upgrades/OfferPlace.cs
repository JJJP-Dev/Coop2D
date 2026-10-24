// Which place of an offer an option fills. A banished option is replaced by another
// of the same place, and a reroll keeps the evolution in its place.
public enum OfferPlace : byte
{
    // Place A: another stack of something you have, or an upgrade that asks for something of yours.
    Yours,

    // Places B and C: type, family, rarity and weight.
    Open,

    // Place B on the levels with a guarantee: at least a rare on 5, an epic on 10.
    Guaranteed,

    // Place A from level 9 when an evolution is ready. It survives rerolls and can't be banished.
    Evolution,

    // Place C swapped for a duo. No duo is in the pool yet.
    Duo,

    // Respiro, when nothing else fits a place. Can't be banished.
    Fallback,
}
