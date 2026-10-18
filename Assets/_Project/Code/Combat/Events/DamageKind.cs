// Where a hit comes from, so each client can show it in its own style.
public enum DamageKind : byte
{
    // Test keys and anything without a kind of its own.
    Other,
    Basic,
    Skill,
    Reaction
}
