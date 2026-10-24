public enum FailReason
{
    None,
    Timeout,
    ServerFailed,
    Unreachable,

    // The host let us in at the transport, then rejected our version.
    VersionMismatch
}