using FishNet.Managing;
using UnityEngine;

public class ConnectionService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager _networkManager;

    public void StartHost()
    {
        StartServer();
        StartClient();
    }

    private void Start()
    {
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void OnServerConnectionState(FishNet.Transporting.ServerConnectionStateArgs obj)
    {
        Debug.Log("Server State: " + obj.ConnectionState.ToString());
    }

    private void OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs obj)
    {
        Debug.Log("Client State: " + obj.ConnectionState.ToString());
    }

    public void StartServer()
    {
        _networkManager.ServerManager.StartConnection();
    }

    public void StartClient()
    {
        _networkManager.ClientManager.StartConnection();
    }

    public void SetIPAddress(string address)
    {
        _networkManager.TransportManager.Transport.SetClientAddress(address);
    }
}