using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnService : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private NetworkObject _playerPrefab;

    private PlayerSpawnPoints _spawnPoints;

    private void Start()
    {
        if (_playerPrefab == null)
            Debug.LogError("[PlayerSpawnService] Player prefab is not assigned.");

        // A player needs two things: the connection finished its first scene sync,
        // and it is inside a scene. Each event covers one, and they can come in
        // either order, so both try to spawn and the last one succeeds.
        _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        _networkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
    }

    private void OnDestroy()
    {
        _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        _networkManager.SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;
    }

    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer)
            return;

        foreach (Scene scene in conn.Scenes)
            TrySpawnPlayer(conn, scene);
    }

    // Server only.
    private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs args)
    {
        if (args.Added)
            TrySpawnPlayer(args.Connection, args.Scene);
    }

    private void TrySpawnPlayer(NetworkConnection conn, Scene scene)
    {
        if (!conn.LoadedStartScenes(true))
            return;

        // Already has a player: it is changing scenes, not joining.
        if (conn.FirstObject != null)
            return;

        if (_playerPrefab == null)
            return;

        Vector3 position = GetSpawnPosition(scene);
        NetworkObject player = _networkManager.GetPooledInstantiated(_playerPrefab, position, Quaternion.identity, true);

        if (player == null)
            return;

        _networkManager.ServerManager.Spawn(player, conn, scene);
    }

    private Vector3 GetSpawnPosition(Scene scene)
    {
        if (_spawnPoints == null || _spawnPoints.gameObject.scene != scene)
        {
            Debug.LogError($"[PlayerSpawnService] No PlayerSpawnPoints registered in scene '{scene.name}'. Spawning at the origin.");
            return Vector3.zero;
        }

        return _spawnPoints.GetSpawnPosition();
    }

    public void RegisterSpawnPoints(PlayerSpawnPoints spawnPoints)
    {
        _spawnPoints = spawnPoints;
    }

    public void UnregisterSpawnPoints(PlayerSpawnPoints spawnPoints)
    {
        // A newer scene may have registered its points already.
        if (_spawnPoints == spawnPoints)
            _spawnPoints = null;
    }
}
