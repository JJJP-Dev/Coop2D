using System.Collections.Concurrent;
using System.Collections.Generic;
using FishNet.Managing;
using UnityEngine;

// The launcher's diagnostic events: hitches, performance, ping and errors. They don't complete missions;
// they go in the summary the launcher sends with the results. None is written more than once a second.
// Unlike PerformanceLogger, this runs in every build, because the launcher reads every tester's log.
public class LauncherDiagnostics : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    [Tooltip("A frame longer than this, in milliseconds, is a hitch. The same as PerformanceLogger's.")]
    [SerializeField, Min(1f)] private float _hitchThresholdMs = 100f;

    [Tooltip("Seconds between perf events, while in a match.")]
    [SerializeField, Min(1f)] private float _perfInterval = 60f;

    [Tooltip("Seconds between net events, on clients in a match.")]
    [SerializeField, Min(1f)] private float _netInterval = 30f;

    // Hitches within one second become a single event with the worst one.
    private double _hitchWindowStart = -1;
    private float _worstHitchMs;

    private double _perfWindowStart;
    private int _perfFrames;
    private float _perfWorstMs;
    private double _nextNetAt;
    private bool _wasInMatch;

    // Errors arrive from any thread, so they wait here until the main thread writes them.
    private readonly ConcurrentQueue<(string type, string signature)> _pendingErrors = new();
    private readonly HashSet<string> _reportedErrors = new();

    private LocalPlayerService _localPlayer;

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += OnLogMessage;
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogMessage;
    }

    private void Start()
    {
        if (GameServices.Instance != null)
            _localPlayer = GameServices.Instance.LocalPlayerService;

        if (_networkManager == null)
            Debug.LogError("[LauncherDiagnostics] Network manager is not assigned.", this);
    }

    private void Update()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        float frameMs = Time.unscaledDeltaTime * 1000f;

        CheckHitch(now, frameMs);
        CheckPerf(now, frameMs);
        CheckNet(now);
        WritePendingErrors();
    }

    private void CheckHitch(double now, float frameMs)
    {
        if (frameMs >= _hitchThresholdMs)
        {
            if (_hitchWindowStart < 0)
                _hitchWindowStart = now;

            _worstHitchMs = Mathf.Max(_worstHitchMs, frameMs);
        }

        if (_hitchWindowStart >= 0 && now - _hitchWindowStart >= 1.0)
        {
            LauncherEvents.Hitch(Mathf.RoundToInt(_worstHitchMs), CountEnemies());
            _hitchWindowStart = -1;
            _worstHitchMs = 0f;
        }
    }

    // Counted only while in a match, from the moment the local player appears.
    private void CheckPerf(double now, float frameMs)
    {
        bool inMatch = IsInMatch();

        if (inMatch && !_wasInMatch)
        {
            _perfWindowStart = now;
            _perfFrames = 0;
            _perfWorstMs = 0f;
            _nextNetAt = now + _netInterval;
        }

        _wasInMatch = inMatch;
        if (!inMatch)
            return;

        _perfFrames++;
        _perfWorstMs = Mathf.Max(_perfWorstMs, frameMs);

        double elapsed = now - _perfWindowStart;
        if (elapsed < _perfInterval)
            return;

        LauncherEvents.Perf(_perfFrames / elapsed, Mathf.RoundToInt(_perfWorstMs), CountEnemies());

        _perfWindowStart = now;
        _perfFrames = 0;
        _perfWorstMs = 0f;
    }

    // Only clients: the host has no ping to itself.
    private void CheckNet(double now)
    {
        if (!_wasInMatch || _networkManager == null || _networkManager.IsServerStarted || now < _nextNetAt)
            return;

        _nextNetAt = now + _netInterval;
        LauncherEvents.Net((int)_networkManager.TimeManager.RoundTripTime);
    }

    private void WritePendingErrors()
    {
        while (_pendingErrors.TryDequeue(out (string type, string signature) error))
        {
            if (_reportedErrors.Add(error.type + error.signature))
                LauncherEvents.LogError(error.type, error.signature, 1);
        }
    }

    // Any thread. Only the signature is kept: the launcher gets no error text.
    private void OnLogMessage(string message, string stackTrace, LogType type)
    {
        string typeId = type switch
        {
            LogType.Error => "error",
            LogType.Exception => "exception",
            LogType.Assert => "assert",
            _ => null,
        };

        if (typeId != null)
            _pendingErrors.Enqueue((typeId, Signature(message)));
    }

    // FNV-1a, 32 bits, of the message's first line, as 8 hex characters.
    private static string Signature(string message)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;

        if (message != null)
        {
            foreach (char c in message)
            {
                if (c == '\n' || c == '\r')
                    break;

                hash ^= c;
                hash *= prime;
            }
        }

        return hash.ToString("x8");
    }

    private bool IsInMatch()
    {
        return _localPlayer != null && _localPlayer.Current != null;
    }

    private static int CountEnemies()
    {
        return FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
    }
}
