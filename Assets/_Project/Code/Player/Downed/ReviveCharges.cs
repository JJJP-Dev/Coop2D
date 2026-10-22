using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The revives the whole group has left in this match (#79), the same on every client.
// The host counts the players, decides how many charges there are and spends them; the clients only
// show them. One per match scene, next to MatchClock. A scene without it has no limit, like ReactionLab.
public class ReviveCharges : NetworkBehaviour
{
    // Players join and leave rarely, so counting them twice a second is plenty.
    private const float CountInterval = 0.5f;

    [SerializeField] private ReviveSettings _settings;

    // Both travel: the total grows when someone joins, and Used also sets how long the next revive takes.
    private readonly SyncVar<int> _total = new();
    private readonly SyncVar<int> _used = new();

    // Server side.
    private int _mostPlayers;
    private float _nextCountTime;

    /// <summary>
    /// The charges of the match this PC is in, on the host and on every client. Null outside a match
    /// scene, or in a scene that doesn't limit revives.
    /// </summary>
    public static ReviveCharges Current { get; private set; }

    public int Total => _total.Value;
    public int Used => _used.Value;
    public int Remaining => Mathf.Max(0, _total.Value - _used.Value);

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[ReviveCharges] Revive settings are not assigned. Nobody will be able to revive.", this);
    }

    // OnStartNetwork runs once on the host, not once as server and again as client,
    // so Current is there for the host's server code and for every client.
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Current = this;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (Current == this)
            Current = null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // A new match starts with every charge, whatever the last one used (#52 will restart here).
        _mostPlayers = 0;
        _nextCountTime = 0f;
        _used.Value = 0;
        _total.Value = 0;
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        if (Time.time < _nextCountTime)
            return;

        _nextCountTime = Time.time + CountInterval;

        int players = CountPlayers();
        if (players <= _mostPlayers)
            return;

        // Only up: someone leaving keeps the charges their arrival gave (#79).
        _mostPlayers = players;
        _total.Value = _settings.GetCharges(players);
    }

    /// <summary>
    /// Server only. Every charge back, for a restarted match (#52). The players who are here now are
    /// the most it has had: someone who left before the restart no longer counts.
    /// </summary>
    public void ResetForNewRun()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        _mostPlayers = CountPlayers();
        _used.Value = 0;
        _total.Value = _settings.GetCharges(_mostPlayers);
    }

    /// <summary>
    /// Server only. Uses one charge if there is one left. False if the group has none.
    /// </summary>
    public bool TrySpend()
    {
        if (!IsServerInitialized || Remaining <= 0)
            return false;

        _used.Value++;
        return true;
    }

    // Players, not connections: a connection that is still being let in (or turned away for its
    // version) has no player yet and must not raise the charges.
    private int CountPlayers()
    {
        int players = 0;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            if (connection.FirstObject != null)
                players++;
        }

        return players;
    }
}
