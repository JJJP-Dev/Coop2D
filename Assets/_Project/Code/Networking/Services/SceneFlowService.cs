using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneFlowService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager _networkManager;

    [SerializeField]
    private string _startingSceneName;

    [SerializeField]
    private string _menuSceneName;

    // Set for a single host, like the reaction test scene (#147). Empty: the starting scene.
    private string _nextHostSceneName;

    private void OnEnable()
    {
        // Only the server will load Scenes
        GameServices.Instance.ConnectionService.ServerStarted += LoadStartingScene;

        GameServices.Instance.ConnectionService.ClientConnectionStateChanged += OnClientConnectionState;
    }

    private void OnDisable()
    {
        if (GameServices.Instance != null)
        {
            GameServices.Instance.ConnectionService.ServerStarted -= LoadStartingScene;

            GameServices.Instance.ConnectionService.ClientConnectionStateChanged -= OnClientConnectionState;
        }
    }

    private void Start()
    {
        // A build only loads scene 0 (Bootstrap) by itself. In the editor the menu
        // may already be open next to Bootstrap, so load it only if it isn't.
        if (!UnitySceneManager.GetSceneByName(_menuSceneName).isLoaded)
            LoadMenuScene();
    }

    /// <summary>
    /// The next host opens this scene instead of the starting one. Only that host: the one after goes back to normal.
    /// </summary>
    public void SetSceneForNextHost(string sceneName)
    {
        _nextHostSceneName = sceneName;
    }

    void LoadStartingScene()
    {
        string sceneName = string.IsNullOrEmpty(_nextHostSceneName) ? _startingSceneName : _nextHostSceneName;
        _nextHostSceneName = null;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneFlowService] Scene '{sceneName}' can't be loaded. Is it in the Build Profile's scene list?");
            return;
        }

        SceneLoadData loadData = new SceneLoadData(sceneName);
        // If you don't replace All, Bootstrap was not loaded by FishNet so it wont be unloaded
        loadData.ReplaceScenes = ReplaceOption.All;
        _networkManager.SceneManager.LoadGlobalScenes(loadData);
    }

    void OnClientConnectionState(SessionState newState)
    {
        if (newState == SessionState.Lost || newState == SessionState.Failed || newState == SessionState.Disconnected)
        {
            // A host that never started doesn't carry its scene over to the next try.
            _nextHostSceneName = null;

            if (UnitySceneManager.GetActiveScene().name != _menuSceneName)
                LoadMenuScene();
        }
    }

    private void LoadMenuScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_menuSceneName))
        {
            Debug.LogError("[SceneFlowService] This Menu Scene name is not valid.");
            return;
        }

        UnitySceneManager.LoadScene(_menuSceneName, LoadSceneMode.Single);
    }
}
