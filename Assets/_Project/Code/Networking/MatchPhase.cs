// Where the match in a scene is (#52). Travels as a byte: add new values at the end.
public enum MatchPhase : byte
{
    Playing,

    // Every player is downed. Waves stop until the host restarts.
    Over,
}
