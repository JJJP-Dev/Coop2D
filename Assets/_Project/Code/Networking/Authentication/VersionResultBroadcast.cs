using FishNet.Broadcast;

// The host's answer to a VersionBroadcast. On a mismatch it carries the host's version,
// so the client can say which one it needs.
public struct VersionResultBroadcast : IBroadcast
{
    public bool Passed;
    public string HostVersion;
}
