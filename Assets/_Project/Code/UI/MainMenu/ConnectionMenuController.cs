using System.Net;
using TMPro;
using UnityEngine;

public class ConnectionMenuController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _ipField;
    private ConnectionService _connectionService;

    private void Start()
    {
        _connectionService = FindAnyObjectByType<ConnectionService>();

        if (_connectionService == null )
        {
            Debug.LogError("No ConnectionService found in scene");
        }
    }

    public void OnHostBtn()
    {
        _connectionService.StartHost();
    }

    public void OnClientBtn()
    {
        string input = _ipField.text;

        string[] parts = input.Split('.');

        if (parts.Length != 4)
        {
            Debug.LogError("Invalid IP");
            return;
        }

        if (!IPAddress.TryParse(input, out IPAddress address))
        {
            Debug.LogError("Invalid IP");
            return;
        }

        _connectionService.SetIPAddress(address.ToString().Trim());
        _connectionService.StartClient();
    }
}
