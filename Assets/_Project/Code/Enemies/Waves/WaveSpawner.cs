using System.Collections.Generic;
using System.Globalization;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Spawns the enemies of a WaveSchedule over time. Only the host runs it: FishNet
// gives every client the enemies it spawns, so the waves match everywhere.
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField] private WaveSchedule _schedule;
    [SerializeField] private Transform[] _spawnPoints;

    private readonly List<Transform> _farEnoughPoints = new();
    private int[] _spawnedPerWave;

    // How many waves have started and whether the schedule is over, for every client.
    // LauncherEventReporter turns them into wave_started and waves_finished: as values rather than
    // messages, they also reach the host's own client, which connects after the first wave starts.
    private readonly SyncVar<int> _wavesStarted = new();
    private readonly SyncVar<bool> _wavesFinished = new();

    // Server side: which waves were already counted.
    private bool[] _counted;

    /// <summary>
    /// The waves of the match this client is in. Null outside a match scene.
    /// </summary>
    public static WaveSpawner Current { get; private set; }

    /// <summary>
    /// Waves that have started since the schedule began, the n-th to start being wave n.
    /// The cheats panel can start the schedule again, and then it goes back to 0.
    /// </summary>
    public int WavesStarted => _wavesStarted.Value;

    /// <summary>
    /// True once the schedule's time is up and no more enemies will come.
    /// </summary>
    public bool WavesFinished => _wavesFinished.Value;
    private float _elapsed;
    private int _aliveEnemies;

    // Server side. While paused the schedule's clock stops and nothing spawns.
    private bool _paused;

    private void Awake()
    {
        if (_schedule == null)
            Debug.LogError("[WaveSpawner] Wave schedule is not assigned.", this);

        if (_spawnPoints == null || _spawnPoints.Length == 0)
            Debug.LogError("[WaveSpawner] No spawn points assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _elapsed = 0f;
        _aliveEnemies = 0;
        _spawnedPerWave = null;
        _paused = false;

        if (_schedule == null || _spawnPoints == null || _spawnPoints.Length == 0)
            return;

        IReadOnlyList<WaveEntry> waves = _schedule.Waves;
        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i].EnemyPrefab == null)
                Debug.LogError($"[WaveSpawner] Wave {i} of {_schedule.name} has no enemy prefab. It will be skipped.", this);
        }

        _spawnedPerWave = new int[waves.Count];
        _counted = new bool[waves.Count];
        _wavesStarted.Value = 0;
        _wavesFinished.Value = false;
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

    private void Update()
    {
        // Null on clients, and when the setup is missing something.
        if (!IsServerInitialized || _spawnedPerWave == null || _paused)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed > _schedule.Duration)
        {
            if (!_wavesFinished.Value)
                _wavesFinished.Value = true;

            return;
        }

        CountStartedWaves();

        IReadOnlyList<WaveEntry> waves = _schedule.Waves;
        for (int i = 0; i < waves.Count; i++)
            SpawnDueEnemies(waves[i], i);
    }

    /// <summary>
    /// Server only. Stops the schedule where it is, or lets it go on. MatchFlow pauses it when the match
    /// ends, so no more enemies come while everyone is downed.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (IsServerInitialized)
            _paused = paused;
    }

    /// <summary>
    /// Server only, for the cheats panel. Jumps to the start of the next wave and drops
    /// what was left of the ones already started, so they don't all come out at once.
    /// False if no wave starts later.
    /// </summary>
    public bool TrySkipToNextWave(out float startTime)
    {
        startTime = 0f;

        if (!IsServerInitialized || _spawnedPerWave == null)
            return false;

        IReadOnlyList<WaveEntry> waves = _schedule.Waves;
        float next = float.MaxValue;

        foreach (WaveEntry wave in waves)
        {
            if (wave.EnemyPrefab != null && wave.StartTime > _elapsed && wave.StartTime < next)
                next = wave.StartTime;
        }

        if (next > _schedule.Duration)
            return false;

        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i].StartTime < next)
                _spawnedPerWave[i] = waves[i].Count;
        }

        _elapsed = next;
        startTime = next;
        return true;
    }

    /// <summary>
    /// Server only, for the cheats panel. Starts the schedule again from the beginning.
    /// The enemies still alive stay and count against the cap, so kill them first.
    /// </summary>
    public void RestartWaves()
    {
        if (!IsServerInitialized || _spawnedPerWave == null)
            return;

        _elapsed = 0f;
        System.Array.Clear(_spawnedPerWave, 0, _spawnedPerWave.Length);
        System.Array.Clear(_counted, 0, _counted.Length);
        _wavesStarted.Value = 0;
        _wavesFinished.Value = false;
    }

    /// <summary>
    /// Server only. The schedule from the beginning, for a restarted match. Call it after
    /// despawning every enemy: despawned enemies never die, so the count of living ones starts at 0.
    /// </summary>
    public void ResetForNewRun()
    {
        if (!IsServerInitialized || _spawnedPerWave == null)
            return;

        RestartWaves();
        _aliveEnemies = 0;
        _paused = false;
    }

    // Also after the cheats panel jumps ahead: the jump only moves the clock, and this sees it on the next frame.
    private void CountStartedWaves()
    {
        IReadOnlyList<WaveEntry> waves = _schedule.Waves;

        for (int i = 0; i < waves.Count; i++)
        {
            if (_counted[i] || waves[i].EnemyPrefab == null || _elapsed < waves[i].StartTime)
                continue;

            _counted[i] = true;
            _wavesStarted.Value++;
        }
    }

    // Spawns every enemy of the wave whose time has come, while there is room.
    // Those held back by the cap come out as soon as others die.
    private void SpawnDueEnemies(WaveEntry wave, int index)
    {
        if (wave.EnemyPrefab == null)
            return;

        while (_spawnedPerWave[index] < wave.Count
               && _elapsed >= wave.StartTime + _spawnedPerWave[index] * wave.SpawnInterval
               && _aliveEnemies < _schedule.MaxAliveEnemies)
        {
            SpawnEnemy(wave.EnemyPrefab);
            _spawnedPerWave[index]++;
        }
    }

    private void SpawnEnemy(Enemy prefab)
    {
        Enemy enemy = Instantiate(prefab, PickSpawnPosition(), Quaternion.identity);

        // Into the arena's scene, so the enemies go away with it.
        Spawn(enemy.gameObject, null, gameObject.scene);

        enemy.GetComponent<Health>().Died += OnEnemyDied;
        _aliveEnemies++;
    }

    private void OnEnemyDied(Health health, NetworkObject attacker)
    {
        health.Died -= OnEnemyDied;
        _aliveEnemies--;
    }

    // A random spawn point far enough from every player, or the farthest one if none is.
    // The roll comes from the run's seed, and always one per enemy, drawn before looking at the
    // players: where they stand changes which points are far enough, but never how many numbers are
    // used, so two runs with the same seed stay in step even when the players move differently.
    private Vector2 PickSpawnPosition()
    {
        RunRandomService randomService = GameServices.Instance.RunRandomService;
        float roll = randomService.GetStream(RandomStream.Waves).Value();

        float minSqrDistance = _schedule.MinDistanceFromPlayers * _schedule.MinDistanceFromPlayers;
        Transform farthest = null;
        float farthestSqrDistance = -1f;

        _farEnoughPoints.Clear();

        foreach (Transform point in _spawnPoints)
        {
            if (point == null)
                continue;

            float sqrDistance = SqrDistanceToNearestPlayer(point.position);

            if (sqrDistance >= minSqrDistance)
                _farEnoughPoints.Add(point);

            if (sqrDistance > farthestSqrDistance)
            {
                farthestSqrDistance = sqrDistance;
                farthest = point;
            }
        }

        // The roll is below 1, so the index is below Count; Min only guards against float rounding.
        Transform chosen = _farEnoughPoints.Count > 0
            ? _farEnoughPoints[Mathf.Min((int)(roll * _farEnoughPoints.Count), _farEnoughPoints.Count - 1)]
            : farthest;

        // Invariant, so two PCs with different languages write the same line.
        if (randomService.LogRolls)
        {
            string rollText = roll.ToString("0.000", CultureInfo.InvariantCulture);
            Debug.Log($"[WaveSpawner] Spawn roll {rollText} -> {(chosen != null ? chosen.name : "spawner")}", this);
        }

        return chosen != null ? chosen.position : transform.position;
    }

    private float SqrDistanceToNearestPlayer(Vector2 position)
    {
        float nearest = float.MaxValue;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null)
                continue;

            nearest = Mathf.Min(nearest, ((Vector2)player.transform.position - position).sqrMagnitude);
        }

        return nearest;
    }
}
