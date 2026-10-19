using FishNet.Broadcast;

// Sent by every client as soon as it connects, the host's own client included.
// FishNet serializes public fields, so this struct can't be read-only.
public struct VersionBroadcast : IBroadcast
{
    public string Version;
}
