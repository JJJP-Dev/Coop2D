using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionMenuController : MonoBehaviour
{
    // Where the last address someone tried to join is kept between sessions.
    // In the Windows registry, under the company and product names of the Player settings.
    private const string LastAddressKey = "LastJoinAddress";

    private ConnectionService _connectionService;

    // The join attempt waiting for its result, for the launcher's join_result.
    private bool _joinPending;
    private string _joinTarget;
    private double _joinStartedAt;

    [SerializeField]
    private TMP_InputField _ipField;

    [SerializeField]
    private Button _hostBtn;
    [SerializeField]
    private Button _clientBtn;

    [Tooltip("Optional. Hosts straight into the reaction test scene. Hidden outside development builds.")]
    [SerializeField]
    private Button _reactionLabBtn;

    [SerializeField]
    private string _reactionLabSceneName = "ReactionLab";

    private void Awake()
    {
        _connectionService = GameServices.Instance.ConnectionService;

        if (_reactionLabBtn != null && !Debug.isDebugBuild)
            _reactionLabBtn.gameObject.SetActive(false);

        // Only into an empty field: whatever is already written there wins.
        if (string.IsNullOrEmpty(_ipField.text))
            _ipField.text = PlayerPrefs.GetString(LastAddressKey, string.Empty);
    }

    private void OnEnable()
    {
        _connectionService.ClientConnectionStateChanged += OnClientConnectionState;
        _ipField.onSubmit.AddListener(OnIpSubmitted);
    }

    private void OnDisable()
    {
        _connectionService.ClientConnectionStateChanged -= OnClientConnectionState;
        _ipField.onSubmit.RemoveListener(OnIpSubmitted);
    }

    // Enter in the IP field acts like the Join button, but only when the button could be pressed.
    private void OnIpSubmitted(string text)
    {
        if (_clientBtn.interactable)
            TryJoin("enter");
    }

    void OnClientConnectionState(SessionState newState)
    {
        ReportJoinResult(newState);

        if (newState == SessionState.Connecting)
        {
            SetButtonsInteractable(false);
        }
        else if (newState == SessionState.Failed || newState == SessionState.Disconnected)
        {
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _hostBtn.interactable = interactable;
        _clientBtn.interactable = interactable;

        if (_reactionLabBtn != null)
            _reactionLabBtn.interactable = interactable;
    }

    public void OnHostBtn()
    {
        _connectionService.StartHost();
    }

    // Other players join it like any other match: the host decides the scene.
    public void OnReactionLabBtn()
    {
        GameServices.Instance.SceneFlowService.SetSceneForNextHost(_reactionLabSceneName);
        _connectionService.StartHost();
    }

    public void OnClientBtn()
    {
        TryJoin("button");
    }

    // via is how it was asked for, for the launcher: button or enter.
    private void TryJoin(string via)
    {
        string input = _ipField.text.Trim();

        string[] parts = input.Split('.');

        if (parts.Length != 4 || !IPAddress.TryParse(input, out IPAddress address))
        {
            LauncherEvents.JoinAttempt(LauncherAddressKind.Invalid, via);
            LauncherEvents.JoinResult("invalid_ip", LauncherAddressKind.Invalid, 0);

            GameServices.Instance.ToastService.Show("IP no válida", ToastType.Warning);
            return;
        }

        _joinPending = true;
        _joinTarget = LauncherAddressKind.Of(address);
        _joinStartedAt = Time.realtimeSinceStartupAsDouble;
        LauncherEvents.JoinAttempt(_joinTarget, via);

        // Saved on every valid attempt, not only on success: if the host is down,
        // the address is still there for the next try.
        PlayerPrefs.SetString(LastAddressKey, address.ToString());
        PlayerPrefs.Save();

        _connectionService.SetIPAddress(address.ToString());
        _connectionService.StartClient();
    }

    // One join_result per join_attempt. The host's own client never starts here, so it never reports one.
    private void ReportJoinResult(SessionState state)
    {
        if (!_joinPending || state == SessionState.Connecting)
            return;

        _joinPending = false;

        string result = state switch
        {
            SessionState.Connected => "ok",
            SessionState.Failed => _connectionService.LastFailReason switch
            {
                FailReason.VersionMismatch => "rejected",
                FailReason.Timeout or FailReason.Unreachable => "not_found",
                _ => "error",
            },
            _ => "error",
        };

        int milliseconds = (int)((Time.realtimeSinceStartupAsDouble - _joinStartedAt) * 1000.0);
        LauncherEvents.JoinResult(result, _joinTarget, milliseconds);
    }
}
