// What an attack's element did to an enemy's aura.
public enum AuraApplyResult : byte
{
    // Nothing: another element that can't detonate, the source's cooldown, or a dead enemy.
    Ignored,
    Applied,
    Refreshed,

    // It consumed the aura of another element and left none of its own.
    Reacted
}
