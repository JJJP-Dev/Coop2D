using FishNet.Managing;
using FishNet.Managing.Scened;
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
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    }

    private void OnDestroy()
    {
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == LocalConnectionState.Started)
        {
            ServerStarted?.Invoke();
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

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!_hostPending || !args.QueueData.AsServer)
            return;

        _hostPending = false;
        StartClient();
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
            _networkManager.ClientManager.StopConnection();
        }
    }

    public void StartServer()
    {
        _networkManager.ServerManager.StartConnection();
    }

    public void StartClient()
    {
        LastFailReason = FailReason.None;
        _leaveRequested = false;
        _networkManager.ClientManager.StartConnection();
    }

    public void SetIPAddress(string address)
    {
        _networkManager.TransportManager.Transport.SetClientAddress(address);
    }

    public void LeaveSession()
    {
        _leaveRequested = true;

        if (_networkManager.IsServerStarted)
        {
            _networkManager.ServerManager.StopConnection(true);
        }

        if (_networkManager.IsClientStarted)
        {
            _networkManager.ClientManager.StopConnection();
        }
    }
}