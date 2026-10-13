public readonly struct NetworkStats
{
    public readonly bool IsHost;
    public readonly long PingMs;
    public readonly int PlayerCount;

    public NetworkStats(bool isHost, long pingMs, int playerCount)
    {
        IsHost = isHost;
        PingMs = pingMs;
        PlayerCount = playerCount;
    }
}
