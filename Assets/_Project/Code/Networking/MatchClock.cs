using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// How long the match in this scene has been going, the same on every client.
// The host keeps the network tick it started on, and each client counts from there with its own
// copy of the tick, which FishNet keeps in step with the host's. One per match scene.
public class MatchClock : NetworkBehaviour
{
    private readonly SyncVar<uint> _startTick = new();

    /// <summary>
    /// The clock of the match this client is in. Null outside a match scene.
    /// </summary>
    public static MatchClock Current { get; private set; }

    /// <summary>
    /// Whole seconds since the host opened the match.
    /// </summary>
    public int ElapsedSeconds => IsSpawned ? Mathf.FloorToInt((float)TimeManager.TimePassed(_startTick.Value)) : 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _startTick.Value = TimeManager.Tick;
    }

    /// <summary>
    /// Server only. Counts again from 0, for a restarted match.
    /// </summary>
    public void Restart()
    {
        if (IsServerInitialized)
            _startTick.Value = TimeManager.Tick;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Current = this;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (Current == this)
            Current = null;
    }
}
