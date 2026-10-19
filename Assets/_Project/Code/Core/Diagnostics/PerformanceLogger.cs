using System;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.Profiling;

// For tests between PCs, in development builds: a line in the log for every frame that takes too long,
// and a summary every few seconds. Without call stacks, so it costs almost nothing.
// Both players' logs, lined up by their clocks, show who froze and what was going on.
[DefaultExecutionOrder(10000)]
public class PerformanceLogger : MonoBehaviour
{
    private const float BytesPerKilobyte = 1024f;
    private const float BytesPerMegabyte = 1024f * 1024f;

    [SerializeField] private NetworkManager _networkManager;

    [Tooltip("A frame longer than this, in milliseconds, gets its own line.")]
    [SerializeField, Min(1f)] private float _hitchThresholdMs = 100f;

    [Tooltip("Seconds between summaries. 0 writes none.")]
    [SerializeField, Min(0f)] private float _summaryInterval = 10f;

    [Tooltip("Also in the editor. Off, so the Console stays quiet.")]
    [SerializeField] private bool _inEditor;

    private CombatEventService _events;
    private Transport _transport;

    // From one LateUpdate to the next: one frame.
    private double _frameStart;
    private int _frameCollections;
    private int _physicsSteps;
    private int _ticks;
    private int _hits;
    private int _deaths;
    private int _reactions;
    private int _chains;
    private long _bytesReceived;

    // Since the last summary.
    private double _summaryStart;
    private int _summaryCollections;
    private int _summaryFrames;
    private int _summaryHitches;
    private float _worstFrameMs;
    private int _summaryHits;
    private int _summaryDeaths;
    private int _summaryReactions;
    private int _summaryChains;
    private long _summaryBytes;

    private void Awake()
    {
        // Release builds don't log any of this.
        if (!Debug.isDebugBuild || (Application.isEditor && !_inEditor))
        {
            enabled = false;
            return;
        }

        if (_networkManager == null)
            Debug.LogError("[PerformanceLogger] Network manager is not assigned.", this);
    }

    private void Start()
    {
        if (GameServices.Instance != null)
        {
            _events = GameServices.Instance.CombatEventService;
            _events.HitConfirmed += OnHit;
            _events.DeathConfirmed += OnDeath;
            _events.ReactionTriggered += OnReaction;
            _events.ChainTriggered += OnChain;
        }

        if (_networkManager != null)
        {
            _networkManager.TimeManager.OnTick += OnTick;

            _transport = _networkManager.TransportManager.Transport;
            _transport.OnServerReceivedData += OnServerReceivedData;
            _transport.OnClientReceivedData += OnClientReceivedData;
        }

        _frameStart = _summaryStart = Time.realtimeSinceStartupAsDouble;
        _frameCollections = _summaryCollections = GC.CollectionCount(0);

        Log($"[Perf] Started at {Clock()}. Version {Application.version}, {SystemInfo.processorType} " +
            $"({SystemInfo.processorCount} threads), {SystemInfo.systemMemorySize} MB of RAM, {SystemInfo.graphicsDeviceName}, " +
            $"{Screen.width}x{Screen.height}. Hitches from {_hitchThresholdMs:0} ms.");
    }

    private void OnDestroy()
    {
        if (_events != null)
        {
            _events.HitConfirmed -= OnHit;
            _events.DeathConfirmed -= OnDeath;
            _events.ReactionTriggered -= OnReaction;
            _events.ChainTriggered -= OnChain;
        }

        if (_networkManager != null && _networkManager.TimeManager != null)
            _networkManager.TimeManager.OnTick -= OnTick;

        if (_transport != null)
        {
            _transport.OnServerReceivedData -= OnServerReceivedData;
            _transport.OnClientReceivedData -= OnClientReceivedData;
        }
    }

    // Each physics step. Many in one frame means the physics was catching up with a slow frame.
    private void FixedUpdate()
    {
        _physicsSteps++;
    }

    // Measured from one LateUpdate to the next, so a frame includes the drawing of the one before
    // and everything this one did before LateUpdate.
    private void LateUpdate()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        float frameMs = (float)((now - _frameStart) * 1000.0);
        int collections = GC.CollectionCount(0);

        if (frameMs >= _hitchThresholdMs)
        {
            _summaryHitches++;
            LogHitch(frameMs, collections - _frameCollections);
        }

        _summaryFrames++;
        _worstFrameMs = Mathf.Max(_worstFrameMs, frameMs);
        _summaryHits += _hits;
        _summaryDeaths += _deaths;
        _summaryReactions += _reactions;
        _summaryChains += _chains;
        _summaryBytes += _bytesReceived;

        _frameStart = now;
        _frameCollections = collections;
        _physicsSteps = 0;
        _ticks = 0;
        _hits = 0;
        _deaths = 0;
        _reactions = 0;
        _chains = 0;
        _bytesReceived = 0;

        if (_summaryInterval > 0f && now - _summaryStart >= _summaryInterval)
            LogSummary(now, collections);
    }

    private void LogHitch(float frameMs, int collections)
    {
        Log($"[Perf] Hitch at {Clock()}: {frameMs:0} ms. {Connection()}. Enemies {Count<Enemy>()}. " +
            $"Physics steps {_physicsSteps}, network ticks {_ticks}, garbage collections {collections}. " +
            $"Events: {_hits} hits, {_deaths} deaths, {_reactions} reactions, {_chains} chains. " +
            $"Received {_bytesReceived / BytesPerKilobyte:0.0} KB. {Memory()}.");
    }

    private void LogSummary(double now, int collections)
    {
        float seconds = (float)(now - _summaryStart);

        Log($"[Perf] Summary at {Clock()}, last {seconds:0} s: {_summaryFrames / seconds:0} fps, worst frame {_worstFrameMs:0} ms, " +
            $"{_summaryHitches} hitches. {Connection()}. Enemies {Count<Enemy>()}, projectiles {Count<Projectile>()}, " +
            $"objects {CountObjects()}. Per second: {_summaryHits / seconds:0.0} hits, {_summaryDeaths / seconds:0.0} deaths, " +
            $"{_summaryReactions / seconds:0.0} reactions, {_summaryChains / seconds:0.0} chains, " +
            $"{_summaryBytes / BytesPerKilobyte / seconds:0.0} KB received. " +
            $"Garbage collections {collections - _summaryCollections}. {Memory()}.");

        _summaryStart = now;
        _summaryCollections = collections;
        _summaryFrames = 0;
        _summaryHitches = 0;
        _worstFrameMs = 0f;
        _summaryHits = 0;
        _summaryDeaths = 0;
        _summaryReactions = 0;
        _summaryChains = 0;
        _summaryBytes = 0;
    }

    // The wall clock, to line up both players' logs, and the seconds since the game started.
    private static string Clock()
    {
        return $"{DateTime.Now:HH:mm:ss.fff} ({Time.realtimeSinceStartup:0.0} s)";
    }

    private string Connection()
    {
        if (_networkManager == null)
            return "No network";

        if (_networkManager.IsServerStarted)
            return $"Host with {_networkManager.ServerManager.Clients.Count} players";

        if (_networkManager.IsClientStarted)
            return $"Client, ping {_networkManager.TimeManager.RoundTripTime} ms";

        return "Offline";
    }

    private static int Count<T>() where T : UnityEngine.Object
    {
        return FindObjectsByType<T>(FindObjectsSortMode.None).Length;
    }

    // Every object, hidden ones included: if it only grows, something isn't being destroyed.
    private static int CountObjects()
    {
        return FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    private static string Memory()
    {
        return $"Memory: {GC.GetTotalMemory(false) / BytesPerMegabyte:0} MB managed, " +
               $"{Profiler.GetTotalAllocatedMemoryLong() / BytesPerMegabyte:0} MB in total";
    }

    private void Log(string line)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, this, "{0}", line);
    }

    private void OnTick()
    {
        _ticks++;
    }

    private void OnHit(HitInfo hit)
    {
        _hits++;
    }

    private void OnDeath(HitInfo hit)
    {
        _deaths++;
    }

    private void OnReaction(ReactionInfo reaction)
    {
        _reactions++;
    }

    private void OnChain(ReactionChain chain)
    {
        _chains++;
    }

    private void OnServerReceivedData(ServerReceivedDataArgs args)
    {
        _bytesReceived += args.Data.Count;
    }

    private void OnClientReceivedData(ClientReceivedDataArgs args)
    {
        _bytesReceived += args.Data.Count;
    }
}
