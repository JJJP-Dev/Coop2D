using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }

    [SerializeField] private ConnectionService _connectionService;
    [SerializeField] private SceneFlowService _sceneFlowService;
    [SerializeField] private ToastService _toastService;
    [SerializeField] private PlayerSpawnService _playerSpawnService;

    public ConnectionService ConnectionService => _connectionService;
    public SceneFlowService SceneFlowService => _sceneFlowService;
    public ToastService ToastService => _toastService;
    public PlayerSpawnService PlayerSpawnService => _playerSpawnService;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
