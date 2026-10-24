using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The group's shared experience and level in this match, the same on every client. Everyone
// fills one bar and goes up together, so nobody competes for orbs; the build is still each player's own.
// The host adds the experience and decides the levels; the clients only show them.
// One per match scene, next to MatchClock.
public class RunExperience : NetworkBehaviour
{
    // Players join and leave rarely, so checking twice a second is plenty.
    private const float PlayerCheckInterval = 0.5f;

    [SerializeField] private XpCurve _curve;

    // The level, the experience inside it and what it needs. The need travels too: it depends on the
    // players there were when the level started, which a client can't know.
    private readonly SyncVar<int> _level = new(1);
    private readonly SyncVar<int> _xp = new();
    private readonly SyncVar<int> _required = new(1);

    // Server side.
    private float _nextPlayerCheck;

    /// <summary>
    /// The experience of the match this PC is in, on the host and on every client. Null outside a match scene.
    /// </summary>
    public static RunExperience Current { get; private set; }

    public int Level => _level.Value;
    public int Xp => _xp.Value;
    public int Required => _required.Value;

    /// <summary>
    /// From 0 to 1: how full the bar of the current level is.
    /// </summary>
    public float Fraction => _required.Value > 0 ? Mathf.Clamp01((float)_xp.Value / _required.Value) : 0f;

    /// <summary>
    /// Server only. The group reached this level. Each level up will give every player an offer.
    /// </summary>
    public event Action<int> LeveledUp;

    private void Awake()
    {
        if (_curve == null)
            Debug.LogError("[RunExperience] Xp curve is not assigned.", this);
    }

    // OnStartNetwork runs once on the host, so Current is there for its server code and for its client.
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
        StartFromLevelOne();
    }

    private void Update()
    {
        if (!IsServerInitialized || _curve == null)
            return;

        if (Time.time < _nextPlayerCheck)
            return;

        _nextPlayerCheck = Time.time + PlayerCheckInterval;

        // A level is fixed by the players there were when it started. Until anyone gets experience in it
        // nobody can tell, so it follows the players: the first level of the match counts everyone who
        // joins in its first seconds, the host's own player included.
        if (_xp.Value == 0)
            SetRequired(_curve.GetRequired(_level.Value, CountPlayers()));
    }

    /// <summary>
    /// Server only. Adds the group's experience, going up as many levels as it fills.
    /// </summary>
    public void AddExperience(int amount)
    {
        if (!IsServerInitialized || _curve == null || amount <= 0)
            return;

        int xp = _xp.Value + amount;
        int level = _level.Value;
        int required = _required.Value;
        int players = CountPlayers();

        while (xp >= required)
        {
            xp -= required;
            level++;

            // Fixed now, with the players there are now: someone joining or leaving changes the
            // next level, not the one the bar is filling.
            required = _curve.GetRequired(level, players);

            _level.Value = level;
            LeveledUp?.Invoke(level);
        }

        _xp.Value = xp;
        SetRequired(required);
    }

    /// <summary>
    /// Server only. Back to level 1 with nothing, for a restarted match.
    /// </summary>
    public void ResetForNewRun()
    {
        if (IsServerInitialized)
            StartFromLevelOne();
    }

    private void StartFromLevelOne()
    {
        _level.Value = 1;
        _xp.Value = 0;
        _nextPlayerCheck = 0f;

        if (_curve != null)
            SetRequired(_curve.GetRequired(1, CountPlayers()));
    }

    private void SetRequired(int required)
    {
        if (_required.Value != required)
            _required.Value = required;
    }

    // Players, not connections: one still being let in has no player yet. At least 1, so the host's
    // first level isn't asked for with nobody there.
    private int CountPlayers()
    {
        int players = 0;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            if (connection.FirstObject != null)
                players++;
        }

        return Mathf.Max(1, players);
    }
}
