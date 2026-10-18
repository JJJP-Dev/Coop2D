using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Cheats for testing, in development builds only. Any player can ask for them,
// and the host checks the build and carries them out, like any other action.
public class DebugCheats : NetworkBehaviour
{
    [SerializeField] private DebugCheatsSettings _settings;

    [Tooltip("Optional: without it, Next Wave only says there are no waves.")]
    [SerializeField] private WaveSpawner _waveSpawner;

    public DebugCheatsSettings Settings => _settings;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[DebugCheats] Cheats settings are not assigned.", this);
    }

    // Called by the panel on its own client. Each one asks the host.
    public void SpawnEnemy(int index)
    {
        if (IsClientInitialized)
            ServerSpawnEnemy(index);
    }

    public void KillAllEnemies()
    {
        if (IsClientInitialized)
            ServerKillAllEnemies();
    }

    public void SkipToNextWave()
    {
        if (IsClientInitialized)
            ServerSkipToNextWave();
    }

    public void ToggleInvulnerable()
    {
        if (IsClientInitialized)
            ServerToggleInvulnerable();
    }

    public void DamageAll()
    {
        if (IsClientInitialized)
            ServerDamageAll();
    }

    public void HealAll()
    {
        if (IsClientInitialized)
            ServerHealAll();
    }

    // Nobody owns this scene object, so it has to allow calls from anyone.
    // FishNet fills in caller with the connection that sent it.
    [ServerRpc(RequireOwnership = false)]
    private void ServerSpawnEnemy(int index, NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        // Only the index travels: the host takes the prefab from its own copy of the settings.
        IReadOnlyList<Enemy> enemies = _settings.Enemies;
        if (index < 0 || index >= enemies.Count || enemies[index] == null)
        {
            Debug.LogWarning($"[DebugCheats] Client {caller.ClientId} asked for enemy {index}, which isn't in the settings.", this);
            return;
        }

        Enemy enemy = Instantiate(enemies[index], PickSpawnPosition(caller), Quaternion.identity);

        // Into the arena's scene, so it goes away with it, like the waves' enemies.
        Spawn(enemy.gameObject, null, gameObject.scene);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerKillAllEnemies(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        // Through their health, so whatever listens for deaths hears them, like the waves' count.
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.IsSpawned && enemy.TryGetComponent(out Health health))
                health.Kill();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerSkipToNextWave(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        if (_waveSpawner == null)
        {
            TargetReport(caller, "No hay oleadas en esta escena", ToastType.Warning);
            return;
        }

        if (_waveSpawner.TrySkipToNextWave(out float startTime))
            TargetReport(caller, $"Saltas a la oleada de {FormatTime(startTime)}", ToastType.Info);
        else
            TargetReport(caller, "No quedan más oleadas", ToastType.Warning);
    }

    // Only the player who asks: each one decides about their own.
    [ServerRpc(RequireOwnership = false)]
    private void ServerToggleInvulnerable(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        NetworkObject player = caller.FirstObject;
        if (player != null && player.TryGetComponent(out Health health))
            health.SetInvulnerable(!health.IsInvulnerable);
    }

    // Players, enemies and dummies alike. The invulnerable ones ignore it.
    [ServerRpc(RequireOwnership = false)]
    private void ServerDamageAll(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (health.IsSpawned)
                health.TryTakeDamage(_settings.DamageAllAmount, null);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerHealAll(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        foreach (Health health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (health.IsSpawned)
                health.RestoreFull();
        }
    }

    // Only to whoever asked: the others don't need to know.
    [TargetRpc]
    private void TargetReport(NetworkConnection target, string message, ToastType type)
    {
        GameServices.Instance.ToastService.Show(message, type);
    }

    // A release build ignores cheats, even from a modified client.
    private bool IsAllowed(NetworkConnection caller)
    {
        if (!Debug.isDebugBuild)
        {
            Debug.LogWarning($"[DebugCheats] Ignored a cheat from client {caller.ClientId}: this isn't a development build.", this);
            return false;
        }

        return _settings != null;
    }

    private Vector2 PickSpawnPosition(NetworkConnection caller)
    {
        // Each connection's first object is its player (see PlayerSpawnService).
        NetworkObject player = caller.FirstObject;
        if (player == null)
            return transform.position;

        float angle = Random.Range(0f, 2f * Mathf.PI);
        Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
        return (Vector2)player.transform.position + direction * _settings.SpawnDistance;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }
}
