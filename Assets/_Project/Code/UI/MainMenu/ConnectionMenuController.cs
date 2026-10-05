using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionMenuController : MonoBehaviour
{
    private ConnectionService _connectionService;

    [SerializeField]
    private TMP_InputField _ipField;

    [SerializeField]
    private Button _hostBtn;
    [SerializeField]
    private Button _clientBtn;

    private void Awake()
    {
        _connectionService = GameServices.Instance.ConnectionService;
    }

    private void OnEnable()
    {
        _connectionService.ClientConnectionStateChanged += OnClientConnectionState;
    }

    private void OnDisable()
    {
        _connectionService.ClientConnectionStateChanged -= OnClientConnectionState;
    }

    void OnClientConnectionState(SessionState newState)
    {
        if (newState == SessionState.Connecting)
        {
            _hostBtn.interactable = false;
            _clientBtn.interactable = false;
        }
        else if (newState == SessionState.Failed || newState == SessionState.Disconnected)
        {
            _hostBtn.interactable = true;
            _clientBtn.interactable = true;
        }
    }

    public void OnHostBtn()
    {
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

        _connectionService.SetIPAddress(address.ToString());
        _connectionService.StartClient();
    }
}
