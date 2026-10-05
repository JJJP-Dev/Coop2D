using FishNet.Managing;
using FishNet.Transporting;
using System;
using System.Collections;
using UnityEngine;

public class ConnectionService : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    public NetworkManager NetworkManager => _networkManager;

    [SerializeField]
    private float _timeoutSeconds = 10;
    private Coroutine _timeoutCoroutine;

    public FailReason LastFailReason { get; private set; }

    public event Action ServerStarted;
    public event Action<SessionState> ClientConnectionStateChanged;

    private bool _leaveRequested = false;
    private SessionState _currentState;

    private bool _hostPending = false;

    public void StartHost()
    {
        LastFailReason = FailReason.None;
        _leaveRequested = false;

        _hostPending = true;

        StartServer();
    }

    private void Start()
    {
        NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        NetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
    }

    private void OnDestroy()
    {
        NetworkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        NetworkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == LocalConnectionState.Started)
        {
            ServerStarted?.Invoke();

            if (_hostPending)
            {
                _hostPending = false;
                StartClient();
            }
        }
        else if (obj.ConnectionState == LocalConnectionState.Stopped)
        {
            if (_hostPending)
            {
                _hostPending = false;
                LastFailReason = FailReason.ServerFailed;
                SetState(SessionState.Failed);
            }
        }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs obj)
    {
        switch (obj.ConnectionState)
        {
            case LocalConnectionState.Stopped:
                if (_leaveRequested)
                    SetState(SessionState.Disconnected);
                else if (_currentState == SessionState.Connecting)
                {
                    if (LastFailReason == FailReason.None)
                        LastFailReason = FailReason.Unreachable;
                    SetState(SessionState.Failed);
                }
                else if (_currentState == SessionState.Connected)
                    SetState(SessionState.Lost);

                _leaveRequested = false;
                break;
            case LocalConnectionState.Stopping:
                break;
            case LocalConnectionState.Starting:
                SetState(SessionState.Connecting);
                break;
            case LocalConnectionState.Started:
                SetState(SessionState.Connected);
                break;
            default:
                break;
        }
    }

    void SetState(SessionState newState)
    {
        if (newState != SessionState.Connecting && _timeoutCoroutine != null)
            StopCoroutine(_timeoutCoroutine);

        // Avoid repeating the same state
        if (newState == _currentState)
            return;
        else if (newState == SessionState.Connecting)
            // Time allowed to be connected
            _timeoutCoroutine = StartCoroutine(CloseAfterDelay(_timeoutSeconds));

        _currentState = newState;

        ClientConnectionStateChanged?.Invoke(newState);

        if (newState == SessionState.Failed)
        {
            Debug.LogWarning("[ConnectionService] New State: " + newState.ToString() + ", Reason: " + LastFailReason);
        }
        else
            Debug.Log("[ConnectionService] New State: " + newState.ToString());
    }

    private IEnumerator CloseAfterDelay(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (_currentState == SessionState.Connecting)
        {
            LastFailReason = FailReason.Timeout;
            NetworkManager.ClientManager.StopConnection();
        }
    }

    public void StartServer()
    {
        NetworkManager.ServerManager.StartConnection();
    }

    public void StartClient()
    {
        LastFailReason = FailReason.None;
        _leaveRequested = false;
        NetworkManager.ClientManager.StartConnection();
    }

    public void SetIPAddress(string address)
    {
        NetworkManager.TransportManager.Transport.SetClientAddress(address);
    }

    public void LeaveSession()
    {
        _leaveRequested = true;

        if (NetworkManager.IsServerStarted)
        {
            NetworkManager.ServerManager.StopConnection(true);
        }

        if (NetworkManager.IsClientStarted)
        {
            NetworkManager.ClientManager.StopConnection();
        }
    }
}