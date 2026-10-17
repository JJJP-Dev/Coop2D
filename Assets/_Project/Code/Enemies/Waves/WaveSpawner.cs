using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Spawns the enemies of a WaveSchedule over time. Only the host runs it: FishNet
// gives every client the enemies it spawns, so the waves match everywhere.
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField] private WaveSchedule _schedule;
    [SerializeField] private Transform[] _spawnPoints;

    private readonly List<Transform> _farEnoughPoints = new();
    private int[] _spawnedPerWave;
    private float _elapsed;
    private int _aliveEnemies;

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

        if (_schedule == null || _spawnPoints == null || _spawnPoints.Length == 0)
            return;

        IReadOnlyList<WaveEntry> waves = _schedule.Waves;
        for (int i = 0; i < waves.Count; i++)
        {
            if (waves[i].EnemyPrefab == null)
                Debug.LogError($"[WaveSpawner] Wave {i} of {_schedule.name} has no enemy prefab. It will be skipped.", this);
        }

        _spawnedPerWave = new int[waves.Count];
    }

    private void Update()
    {
        // Null on clients, and when the setup is missing something.
        if (!IsServerInitialized || _spawnedPerWave == null)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed > _schedule.Duration)
            return;

        IReadOnlyList<WaveEntry> waves = _schedule.Waves;
        for (int i = 0; i < waves.Count; i++)
            SpawnDueEnemies(waves[i], i);
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

    // UnityEngine.Random is temporary: the shared seed (#75) will replace it.
    private Vector2 PickSpawnPosition()
    {
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

        if (_farEnoughPoints.Count > 0)
            return _farEnoughPoints[Random.Range(0, _farEnoughPoints.Count)].position;

        return farthest != null ? farthest.position : transform.position;
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
