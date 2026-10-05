using FishNet.Connection;
using UnityEngine;

public class PlayerSpawnService : MonoBehaviour
{
    private PlayerSpawnPoints _spawnPoints;

    private void Start()
    {
        GameServices.Instance.ConnectionService.NetworkManager.SceneManager.OnClientLoadedStartScenes += ClientLoadedScenes;
    }

    private void OnDestroy()
    {
        GameServices.Instance.ConnectionService.NetworkManager.SceneManager.OnClientLoadedStartScenes -= ClientLoadedScenes;
    }

    void ClientLoadedScenes(NetworkConnection conn, bool asServer)
    {
        if (_spawnPoints == null)


        if (asServer)
        {

        }
    }
}
