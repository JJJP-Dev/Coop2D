using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using System;
using System.Collections;
using UnityEngine;

public class ConnectionService : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    [Tooltip("The one on the NetworkManager's object. It tells this client when the host rejects its version (#24).")]
    [SerializeField] private VersionAuthenticator _versionAuthenticator;

    [SerializeField]
    private float _timeoutSeconds = 10;
    private Coroutine _timeoutCoroutine;

    public FailReason LastFailReason { get; private set; }

    /// <summary>
    /// The host's version, after it rejected ours. Empty otherwise.
    /// </summary>
    public string HostVersion { get; private set; } = string.Empty;

    public event Action ServerStarted;
    public event Action<SessionState> ClientConnectionStateChanged;

    private bool _leaveRequested = false;
    private SessionState _currentState;

    private bool _hostPending = false;

    public bool IsConnected => _currentState == SessionState.Connected;

    public void StartHost()
    {
        LastFailReason = FailReason.None;
        _leaveRequested = false;

        _hostPending = true;

        // Every attempt starts in Connecting, so a second failure in a row
        // is still a change of state and listeners hear about it (#150).
        SetState(SessionState.Connecting);
        StartServer();
    }

    private void Start()
    {
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        _networkManager.ClientManager.OnAuthenticated += OnClientAuthenticated;
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;

        if (_versionAuthenticator != null)
            _versionAuthenticator.Rejected += OnVersionRejected;
        else
            Debug.LogError("[ConnectionService] Version authenticator is not assigned.", this);
    }

    private void OnDestroy()
    {
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        _networkManager.ClientManager.OnAuthenticated -= OnClientAuthenticated;
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;

        if (_versionAuthenticator != null)
            _versionAuthenticator.Rejected -= OnVersionRejected;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == LocalConnectionState.Started)
        {
            if (_hostPending)
                LauncherEvents.HostStarted("ok");

            ServerStarted?.Invoke();
        }
        else if (obj.ConnectionState == LocalConnectionState.Stopped)
        {
            if (_hostPending)
            {
                // The only way a host fails to start today is the port being taken.
                LauncherEvents.HostStarted("port_in_use");
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
            // Connected to the host's transport, but not let in yet: that comes with OnAuthenticated.
            // Until then it stays Connecting, so a rejected version is a failed attempt, not a lost session.
            case LocalConnectionState.Started:
                break;
            default:
                break;
        }
    }

    private void OnClientAuthenticated()
    {
        SetState(SessionState.Connected);
    }

    // The kick arrives right after, and Stopped turns it into a failed attempt with this reason.
    private void OnVersionRejected(string hostVersion)
    {
        HostVersion = hostVersion;
        LastFailReason = FailReason.VersionMismatch;
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
        if (newState != SessionState.Connecting)
            StopTimeout();

        // Avoid repeating the same state
        if (newState == _currentState)
            return;

        _currentState = newState;

        ClientConnectionStateChanged?.Invoke(newState);

        if (newState == SessionState.Failed)
        {
            Debug.LogWarning("[ConnectionService] New State: " + newState.ToString() + ", Reason: " + LastFailReason);
        }
        else
            Debug.Log("[ConnectionService] New State: " + newState.ToString());
    }

    // The timeout covers the client connecting, not the host loading its first scene.
    private void StartTimeout()
    {
        StopTimeout();
        _timeoutCoroutine = StartCoroutine(CloseAfterDelay(_timeoutSeconds));
    }

    private void StopTimeout()
    {
        if (_timeoutCoroutine == null)
            return;

        StopCoroutine(_timeoutCoroutine);
        _timeoutCoroutine = null;
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
        HostVersion = string.Empty;
        _leaveRequested = false;
        StartTimeout();
        _networkManager.ClientManager.StartConnection();
    }

    public NetworkStats GetStats()
    {
        return new NetworkStats(
            _networkManager.IsServerStarted,
            _networkManager.TimeManager.RoundTripTime,
            _networkManager.ClientManager.Clients.Count);
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