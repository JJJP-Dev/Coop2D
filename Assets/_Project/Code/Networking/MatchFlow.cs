using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Whether the match in this scene is being played or over (#52), the same on every client.
// The host ends it when every player is downed: nobody is left to revive anyone, so the charges
// don't matter. The clients only show it. One per match scene, next to MatchClock.
// The host can restart it for everyone. The scene stays loaded: FishNet doesn't load a global scene
// that is already loaded, and the players live in it. Instead the host puts every part of the match
// back to its start in one frame, and each client sees it through the values it already receives.
public class MatchFlow : NetworkBehaviour
{
    // A tenth of a second would be wasted work: a quarter is still far below what players notice.
    private const float CheckInterval = 0.25f;

    [SerializeField] private MatchClock _clock;

    [Tooltip("Optional. Stopped when the match ends, so no more enemies come while the panel is up.")]
    [SerializeField] private WaveSpawner _waves;

    [Tooltip("Optional. Its charges come back on a restart.")]
    [SerializeField] private ReviveCharges _charges;

    [Tooltip("Optional. Back to level 1 on a restart (#76).")]
    [SerializeField] private RunExperience _experience;

    [Tooltip("Optional. Its orbs go away on a restart (#84).")]
    [SerializeField] private ExperienceOrbs _orbs;

    private readonly SyncVar<MatchPhase> _phase = new();

    // What the panel and the launcher show, frozen when the match ends so every client says the same.
    private readonly SyncVar<int> _endSeconds = new();
    private readonly SyncVar<int> _endWaves = new();

    // Grows by one on each restart, so every client can tell a new match started in the same scene.
    private readonly SyncVar<int> _runNumber = new();

    // Server side.
    private float _nextCheckTime;

    /// <summary>
    /// The match of the scene this PC is in, on the host and on every client. Null outside a match scene.
    /// </summary>
    public static MatchFlow Current { get; private set; }

    public MatchPhase Phase => _phase.Value;

    /// <summary>
    /// Seconds the match had lasted when it ended. 0 while it's being played.
    /// </summary>
    public int EndSeconds => _endSeconds.Value;

    /// <summary>
    /// Waves that had started when it ended. 0 while it's being played.
    /// </summary>
    public int EndWaves => _endWaves.Value;

    /// <summary>
    /// 0 for the first match of the scene, then one more on each restart.
    /// </summary>
    public int RunNumber => _runNumber.Value;

    private void Awake()
    {
        if (_clock == null)
            Debug.LogError("[MatchFlow] Match clock is not assigned.", this);
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

        _phase.Value = MatchPhase.Playing;
        _endSeconds.Value = 0;
        _endWaves.Value = 0;
        _runNumber.Value = 0;
        _nextCheckTime = 0f;
    }

    private void Update()
    {
        if (!IsServerInitialized || _phase.Value != MatchPhase.Playing)
            return;

        if (Time.time < _nextCheckTime)
            return;

        _nextCheckTime = Time.time + CheckInterval;

        if (AreAllPlayersDowned())
            EndMatch();
    }

    private void EndMatch()
    {
        _endSeconds.Value = _clock != null ? _clock.ElapsedSeconds : 0;
        _endWaves.Value = _waves != null ? _waves.WavesStarted : 0;
        _phase.Value = MatchPhase.Over;

        if (_waves != null)
            _waves.SetPaused(true);

        Debug.Log($"[MatchFlow] Every player is downed. Match over after {_endSeconds.Value} s and {_endWaves.Value} waves.", this);
    }

    /// <summary>
    /// Server only. Starts the match again for everyone, with a new seed. Only once it's over.
    /// </summary>
    public void RestartMatch()
    {
        if (!IsServerInitialized || _phase.Value != MatchPhase.Over)
            return;

        GameServices services = GameServices.Instance;

        // Despawned, not killed: a kill would show numbers, count deaths and write enemy_killed.
        // Destroyed, not pooled (their NetworkObject's despawn type), so nothing is left behind.
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsSpawned)
                enemy.Despawn();
        }

        // The training dummies stay, as they were when the match started.
        foreach (TrainingDummy dummy in FindObjectsByType<TrainingDummy>(FindObjectsSortMode.None))
            ResetDummy(dummy);

        services.ReactionService.ClearClouds();

        // Before the experience goes back to level 1, so no orb is picked up into the new match.
        if (_orbs != null)
            _orbs.ResetForNewRun();

        if (_waves != null)
            _waves.ResetForNewRun();

        if (_clock != null)
            _clock.Restart();

        // MatchSeed hears the new run and tells every client its seed.
        services.RunRandomService.BeginNewRun();

        ResetPlayers(services.PlayerSpawnService);

        // After the players: the charges and the first level count the players who are here now.
        if (_charges != null)
            _charges.ResetForNewRun();

        if (_experience != null)
            _experience.ResetForNewRun();

        _endSeconds.Value = 0;
        _endWaves.Value = 0;
        _runNumber.Value++;
        _phase.Value = MatchPhase.Playing;

        Debug.Log($"[MatchFlow] Match restarted ({_runNumber.Value}).", this);
    }

    // Full health and back to their spawn point. The revive goes right after the health, in the
    // same frame, so getting up there isn't reported as a revive.
    private void ResetPlayers(PlayerSpawnService spawnService)
    {
        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null)
                continue;

            if (player.TryGetComponent(out Health health))
                health.RestoreFull();

            if (player.TryGetComponent(out PlayerRevive revive))
                revive.ResetForNewRun();

            if (player.TryGetComponent(out PlayerIdentity identity) && player.TryGetComponent(out PlayerMovement movement))
                movement.PlaceAt(spawnService.GetSpawnPosition(gameObject.scene, identity.Slot));
        }
    }

    private static void ResetDummy(TrainingDummy dummy)
    {
        if (!dummy.IsSpawned)
            return;

        if (dummy.TryGetComponent(out Health health))
            health.RestoreFull();

        if (dummy.TryGetComponent(out AuraHolder aura))
            aura.ResetForTesting();
    }

    // False with no players at all: the host's own player arrives a moment after the scene.
    private bool AreAllPlayersDowned()
    {
        int players = 0;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null || !player.TryGetComponent(out PlayerDowned downed))
                continue;

            if (!downed.IsDowned)
                return false;

            players++;
        }

        return players > 0;
    }
}
