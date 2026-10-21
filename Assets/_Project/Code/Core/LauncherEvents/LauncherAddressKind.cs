using System.Net;
using System.Net.Sockets;

// What kind of address a player tried to join, for join_attempt. Never the address itself:
// the launcher's events carry nothing personal.
public static class LauncherAddressKind
{
    public const string Invalid = "invalid";

    public static string Of(IPAddress address)
    {
        if (address == null || address.AddressFamily != AddressFamily.InterNetwork)
            return Invalid;

        byte[] b = address.GetAddressBytes();

        // 127.0.0.0/8
        if (b[0] == 127)
            return "loopback";

        // 10/8, 172.16/12 and 192.168/16
        if (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168))
            return "lan";

        // 25/8 (Hamachi), 26/8 (Radmin) and 100.64/10 (Tailscale and ZeroTier)
        if (b[0] == 25 || b[0] == 26 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127))
            return "vpn";

        return "public";
    }
}
