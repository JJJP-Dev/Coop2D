using System;
using FishNet.Authenticating;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

// Lets in only clients with the host's exact version (#24). FishNet doesn't count a client as
// authenticated, or load scenes for it, until this says so, and it kicks the ones it rejects.
// It goes on the NetworkManager's object: the ServerManager finds it there by itself.
public class VersionAuthenticator : Authenticator
{
    /// <summary>
    /// Server side. FishNet listens to it to let the client in or kick it.
    /// </summary>
    public override event Action<NetworkConnection, bool> OnAuthenticationResult;

    /// <summary>
    /// Client side, raised just before the host kicks this client. The argument is the host's version.
    /// </summary>
    public event Action<string> Rejected;

    public override void InitializeOnce(NetworkManager networkManager)
    {
        base.InitializeOnce(networkManager);

        NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        NetworkManager.ClientManager.RegisterBroadcast<VersionResultBroadcast>(OnVersionResult);

        // false: these clients aren't authenticated yet, and that's what the message is for.
        NetworkManager.ServerManager.RegisterBroadcast<VersionBroadcast>(OnVersionBroadcast, false);
    }

    private void OnDestroy()
    {
        if (NetworkManager == null)
            return;

        NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        NetworkManager.ClientManager.UnregisterBroadcast<VersionResultBroadcast>(OnVersionResult);
        NetworkManager.ServerManager.UnregisterBroadcast<VersionBroadcast>(OnVersionBroadcast);
    }

    // The host's own client goes through this too: its version always matches.
    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState != LocalConnectionState.Started)
            return;

        NetworkManager.ClientManager.Broadcast(new VersionBroadcast { Version = Application.version });
    }

    private void OnVersionBroadcast(NetworkConnection conn, VersionBroadcast message, Channel channel)
    {
        // A second message from someone already in can only be a modified client.
        if (conn.IsAuthenticated)
        {
            conn.Disconnect(true);
            return;
        }

        bool passed = string.Equals(message.Version, Application.version, StringComparison.Ordinal);

        if (!passed)
            Debug.LogWarning($"[VersionAuthenticator] Rejected client {conn.ClientId}: it has version " +
                             $"'{message.Version}' and this host has '{Application.version}'.", this);

        // The answer goes first, so it reaches the client before the kick.
        VersionResultBroadcast result = new() { Passed = passed, HostVersion = Application.version };
        NetworkManager.ServerManager.Broadcast(conn, result, false);

        OnAuthenticationResult?.Invoke(conn, passed);
    }

    private void OnVersionResult(VersionResultBroadcast result, Channel channel)
    {
        if (!result.Passed)
            Rejected?.Invoke(result.HostVersion);
    }
}
