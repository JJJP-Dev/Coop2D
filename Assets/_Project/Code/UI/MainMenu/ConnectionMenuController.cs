using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionMenuController : MonoBehaviour
{
    // Where the last address someone tried to join is kept between sessions (#38).
    // In the Windows registry, under the company and product names of the Player settings.
    private const string LastAddressKey = "LastJoinAddress";

    private ConnectionService _connectionService;

    [SerializeField]
    private TMP_InputField _ipField;

    [SerializeField]
    private Button _hostBtn;
    [SerializeField]
    private Button _clientBtn;

    [Tooltip("Optional. Hosts straight into the reaction test scene (#147). Hidden outside development builds.")]
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
            OnClientBtn();
    }

    void OnClientConnectionState(SessionState newState)
    {
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
        string input = _ipField.text.Trim();

        string[] parts = input.Split('.');

        if (parts.Length != 4)
        {
            GameServices.Instance.ToastService.Show("IP no válida", ToastType.Warning);
            return;
        }

        if (!IPAddress.TryParse(input, out IPAddress address))
        {
            GameServices.Instance.ToastService.Show("IP no válida", ToastType.Warning);
            return;
        }

        // Saved on every valid attempt, not only on success: if the host is down,
        // the address is still there for the next try.
        PlayerPrefs.SetString(LastAddressKey, address.ToString());
        PlayerPrefs.Save();

        _connectionService.SetIPAddress(address.ToString());
        _connectionService.StartClient();
    }
}
