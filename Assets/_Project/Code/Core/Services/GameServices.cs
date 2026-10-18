using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }

    [SerializeField] private ConnectionService _connectionService;
    [SerializeField] private SceneFlowService _sceneFlowService;
    [SerializeField] private ToastService _toastService;
    [SerializeField] private PlayerSpawnService _playerSpawnService;
    [SerializeField] private LocalPlayerService _localPlayerService;
    [SerializeField] private InputService _inputService;
    [SerializeField] private CombatEventService _combatEventService;
    [SerializeField] private ReactionService _reactionService;

    public ConnectionService ConnectionService => _connectionService;
    public SceneFlowService SceneFlowService => _sceneFlowService;
    public ToastService ToastService => _toastService;
    public PlayerSpawnService PlayerSpawnService => _playerSpawnService;
    public LocalPlayerService LocalPlayerService => _localPlayerService;
    public InputService InputService => _inputService;
    public CombatEventService CombatEventService => _combatEventService;
    public ReactionService ReactionService => _reactionService;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ConnectionService == null)
            Debug.LogError("[GameServices] ConnectionService is null.");
        if (SceneFlowService == null)
            Debug.LogError("[GameServices] SceneFlowService is null.");
        if (ToastService == null)
            Debug.LogError("[GameServices] ToastService is null.");
        if (PlayerSpawnService == null)
            Debug.LogError("[GameServices] PlayerSpawnService is null.");
        if (LocalPlayerService == null)
            Debug.LogError("[GameServices] LocalPlayerService is null.");
        if (InputService == null)
            Debug.LogError("[GameServices] InputService is null.");
        if (CombatEventService == null)
            Debug.LogError("[GameServices] CombatEventService is null.");
        if (ReactionService == null)
            Debug.LogError("[GameServices] ReactionService is null.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
