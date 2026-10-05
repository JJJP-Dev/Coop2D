using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

public class PlayerSpawnService : MonoBehaviour
{
    private NetworkManager _networkManager;
    private PlayerSpawnPoints _spawnPoints;

    private void Awake()
    {
        _networkManager.SceneManager.OnClientLoadedStartScenes += ClientLoadedScenes;
    }

    private void OnDestroy()
    {
        _networkManager.SceneManager.OnClientLoadedStartScenes -= ClientLoadedScenes;
    }

    void ClientLoadedScenes(NetworkConnection conn, bool asServer)
    {
        if (_spawnPoints == null)


        if (asServer)
        {

        }
    }
}
