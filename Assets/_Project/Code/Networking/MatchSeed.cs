using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The seed of the match in this scene, the same on every client, so any player can read it to
// report or replay a run. The host copies it from RunRandomService; the clients only show it.
// A SyncVar and not a message: it also reaches the host's own client, which connects after the scene
// loads, and the players who join late. One per match scene, next to MatchClock.
public class MatchSeed : NetworkBehaviour
{
    private readonly SyncVar<uint> _seed = new();

    /// <summary>
    /// The seed of the match this client is in. Null outside a match scene.
    /// </summary>
    public static MatchSeed Current { get; private set; }

    public uint Seed => _seed.Value;

    /// <summary>
    /// The seed as players see it: 8 hexadecimal characters, like 3A9F01C2.
    /// </summary>
    public string SeedText => RunRandomService.FormatSeed(_seed.Value);

    public override void OnStartServer()
    {
        base.OnStartServer();

        // The run started with the server, before this scene loaded, so the seed is already there.
        RunRandomService randomService = GameServices.Instance.RunRandomService;
        randomService.RunStarted += OnRunStarted;
        _seed.Value = randomService.Seed;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (GameServices.Instance != null)
            GameServices.Instance.RunRandomService.RunStarted -= OnRunStarted;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Current = this;

        // FishNet applies the SyncVars of a spawn before OnStartClient, so the value is the host's.
        Debug.Log($"[MatchSeed] Match seed {SeedText}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (Current == this)
            Current = null;
    }

    // Server side. A new run without a new scene, like a restart.
    private void OnRunStarted(uint seed)
    {
        _seed.Value = seed;
    }
}
