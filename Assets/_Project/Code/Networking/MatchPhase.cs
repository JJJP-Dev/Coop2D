// Where the match in a scene is. Travels as a byte: add new values at the end.
public enum MatchPhase : byte
{
    Playing,

    // Lost (every player is downed) or won (MatchFlow.Won). Waves stop until the host restarts.
    Over,
}
