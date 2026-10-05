using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;

public class SceneFlowService : MonoBehaviour
{
    [SerializeField]
    private string _startingSceneName;

    [SerializeField]
    private string _menuSceneName;

    private void OnEnable()
    {
        // Only the server will load Scenes
        GameServices.Instance.ConnectionService.ServerStarted += LoadStartingScene;

        GameServices.Instance.ConnectionService.ClientConnectionStateChanged += OnClientConnectionState;
    }

    private void OnDisable()
    {
        GameServices.Instance.ConnectionService.ServerStarted -= LoadStartingScene;

        GameServices.Instance.ConnectionService.ClientConnectionStateChanged -= OnClientConnectionState;
    }

    void LoadStartingScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_startingSceneName))
        {
            Debug.LogError("[SceneFlowService] This Starting Scene name is not valid.");
            return;
        }

        SceneLoadData loadData = new SceneLoadData(_startingSceneName);
        // If you don't replace All, Bootstrap was not loaded by FishNet so it wont be unloaded
        loadData.ReplaceScenes = ReplaceOption.All;
        GameServices.Instance.ConnectionService.NetworkManager.SceneManager.LoadGlobalScenes(loadData);
    }

    void OnClientConnectionState(SessionState newState)
    {
        if (newState == SessionState.Lost || newState == SessionState.Failed || newState == SessionState.Disconnected)
        {
            if (!Application.CanStreamedLevelBeLoaded(_menuSceneName))
            {
                Debug.LogError("[SceneFlowService] This Menu Scene name is not valid.");
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != _menuSceneName)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_menuSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }
}
