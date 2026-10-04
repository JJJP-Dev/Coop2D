using FishNet.Managing;
using FishNet.Transporting;
using System;
using UnityEngine;

public class ConnectionService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager _networkManager;

    public event Action ServerStarted;

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

    private void OnDestroy()
    {
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == LocalConnectionState.Started)
            ServerStarted?.Invoke();

        Debug.Log("Server State: " + obj.ConnectionState.ToString());
    }

    private void OnClientConnectionState(ClientConnectionStateArgs obj)
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