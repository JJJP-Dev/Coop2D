using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;

public class SceneFlowService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager _networkManager;

    private ConnectionService _connectionService;

    [SerializeField]
    private string _startingSceneName;

    private void Awake()
    {
        _connectionService = GetComponent<ConnectionService>();
    }

    private void OnEnable()
    {
        // Only the server will load Scenes
        _connectionService.ServerStarted += LoadStartingScene;
    }

    private void OnDisable()
    {
        _connectionService.ServerStarted -= LoadStartingScene;
    }

    void LoadStartingScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_startingSceneName))
        {
            Debug.LogError("[SceneFlowService] This Scene name is not valid.");
            return;
        }

        SceneLoadData loadData = new SceneLoadData(_startingSceneName);
        // If you don't replace All, Bootstrap was not loaded by FishNet so it wont be unloaded
        loadData.ReplaceScenes = ReplaceOption.All;
        _networkManager.SceneManager.LoadGlobalScenes(loadData);
    }
}
