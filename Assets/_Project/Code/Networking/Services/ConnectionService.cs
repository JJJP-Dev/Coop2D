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

    /// <summary>
    /// True when the last session was lost because the host closed it, not because the connection dropped.
    /// Read it when the state changes to Lost.
    /// </summary>
    public bool HostClosedSession { get; private set; }

    public event Action ServerStarted;
    public event Action<SessionState> ClientConnectionStateChanged;

    private bool _leaveRequested = false;
    private SessionState _currentState;

    private bool _hostPending = false;

    // The game is closing: no more state changes, so nothing tries to load the menu on the way out.
    private bool _quitting;

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
        _networkManager.ClientManager.RegisterBroadcast<HostClosingBroadcast>(OnHostClosing);

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
        _networkManager.ClientManager.UnregisterBroadcast<HostClosingBroadcast>(OnHostClosing);

        if (_versionAuthenticator != null)
            _versionAuthenticator.Rejected -= OnVersionRejected;
    }

    // Closing the host's window also closes the match on purpose, so its clients hear it too.
    private void OnApplicationQuit()
    {
        if (!_networkManager.IsServerStarted)
            return;

        _quitting = true;
        TellClientsHostIsClosing();
        _networkManager.ServerManager.StopConnection(true);
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

    // Arrives just before the disconnect, on the same reliable channel, so Stopped already knows why.
    private void OnHostClosing(HostClosingBroadcast message, Channel channel)
    {
        HostClosedSession = true;
    }

    // Server side. StopConnection(true) sends whatever is queued before its own disconnect message,
    // so this reaches every client first.
    private void TellClientsHostIsClosing()
    {
        _networkManager.ServerManager.Broadcast(new HostClosingBroadcast());
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
        if (_quitting)
            return;

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
        HostClosedSession = false;
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
            TellClientsHostIsClosing();
            _networkManager.ServerManager.StopConnection(true);
        }

        if (_networkManager.IsClientStarted)
        {
            _networkManager.ClientManager.StopConnection();
        }
    }
}