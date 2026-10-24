using System;

// What is true about the target of a hit, for the bonuses that only count sometimes (ConditionalBlock).
// A set of flags: a hit can meet several at once, and a ConditionalBlock that asks for
// several needs them all. Stored in assets as a number: add new ones with the next power of two.
[Flags]
public enum TargetConditions : byte
{
    None = 0,

    // The enemy had an aura just before the hit, even if the hit consumed it.
    HasAura = 1,
}
