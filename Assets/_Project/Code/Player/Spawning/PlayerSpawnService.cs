using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnService : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private NetworkObject _playerPrefab;

    private readonly PlayerSlots _slots = new();
    private PlayerSpawnPoints _spawnPoints;

    private void Start()
    {
        if (_playerPrefab == null)
            Debug.LogError("[PlayerSpawnService] Player prefab is not assigned.");
        else if (!_playerPrefab.TryGetComponent(out PlayerIdentity _))
            Debug.LogError("[PlayerSpawnService] Player prefab has no PlayerIdentity.");

        // A player needs two things: the connection finished its first scene sync,
        // and it is inside a scene. Each event covers one, and they can come in
        // either order, so both try to spawn and the last one succeeds.
        _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
        _networkManager.SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;

        _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void OnDestroy()
    {
        _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
        _networkManager.SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;

        _networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
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

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
            _slots.Release(conn);
    }

    // When the server stops, FishNet drops every client without a Stopped event for each one.
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
            _slots.Clear();
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

        if (!_slots.TryAssign(conn, out int slot))
        {
            Debug.LogWarning($"[PlayerSpawnService] No free player slot for client {conn.ClientId}. Disconnecting it.");
            conn.Disconnect(true);
            return;
        }

        Vector3 position = GetSpawnPosition(scene, slot);
        NetworkObject player = _networkManager.GetPooledInstantiated(_playerPrefab, position, Quaternion.identity, true);

        if (player == null)
        {
            _slots.Release(conn);
            return;
        }

        player.GetComponent<PlayerIdentity>().SetSlot(slot);

        // Its character, from the slot until the lobby lets players choose.
        if (player.TryGetComponent(out PlayerCharacter character))
            character.SetFromSlot(slot);

        // Joining a match that is already lost: downed like the rest, until the host restarts.
        MatchFlow flow = MatchFlow.Current;
        if (flow != null && flow.ShouldSpawnDowned(slot) && player.TryGetComponent(out Health health))
            health.SpawnDowned();

        _networkManager.ServerManager.Spawn(player, conn, scene);
    }

    /// <summary>
    /// Server only. Where the player of this slot appears in that scene. The restart puts players back here.
    /// </summary>
    public Vector3 GetSpawnPosition(Scene scene, int slot)
    {
        if (_spawnPoints == null || _spawnPoints.gameObject.scene != scene)
        {
            Debug.LogError($"[PlayerSpawnService] No PlayerSpawnPoints registered in scene '{scene.name}'. Spawning at the origin.");
            return Vector3.zero;
        }

        return _spawnPoints.GetSpawnPosition(slot);
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
