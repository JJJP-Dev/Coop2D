using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// The seed of the run and the random streams that come out of it (#75). Everything random that changes
// the game (waves, rooms, upgrade offers) draws from here, so the same seed plays the same run.
// Only the host uses it: it decides everything random and the clients see the results.
// Effects that only look different (particles, shake, floating text) keep UnityEngine.Random, which is
// local and never touches the seed; mixing both would make the same seed give a different run.
public class RunRandomService : MonoBehaviour
{
    [SerializeField] private RunRandomSettings _settings;

    // Created the first time each one is asked for. Every stream depends only on the seed and its number,
    // so the order in which systems ask for them doesn't matter.
    private readonly Dictionary<int, SeededRandom> _streams = new();

    /// <summary>
    /// The seed of the current run. 0 when there is none.
    /// </summary>
    public uint Seed { get; private set; }

    /// <summary>
    /// True from the moment this PC hosts until it leaves. Always false on a client.
    /// </summary>
    public bool HasRun { get; private set; }

    /// <summary>
    /// Whether gameplay should write its rolls to the console (RunRandomSettings), to compare two runs.
    /// </summary>
    public bool LogRolls => _settings != null && _settings.LogRolls && Debug.isDebugBuild;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[RunRandomService] Settings are not assigned. Every run will get a new seed.", this);
    }

    private void OnEnable()
    {
        // The run starts with the server, before it loads the match scene, so nothing draws without a seed.
        GameServices.Instance.ConnectionService.ServerStarted += OnServerStarted;
        GameServices.Instance.ConnectionService.ClientConnectionStateChanged += OnClientConnectionState;
    }

    private void OnDisable()
    {
        if (GameServices.Instance == null)
            return;

        GameServices.Instance.ConnectionService.ServerStarted -= OnServerStarted;
        GameServices.Instance.ConnectionService.ClientConnectionStateChanged -= OnClientConnectionState;
    }

    /// <summary>
    /// Server only. Starts a run with this seed: every stream begins again from it.
    /// Hosting starts one by itself; call it to restart a run (#52), with the same seed or a new one.
    /// </summary>
    public void BeginRun(uint seed)
    {
        Seed = seed;
        HasRun = true;
        _streams.Clear();

        // Also in release builds: a tester can copy it from the log to report a run.
        Debug.Log($"[RunRandomService] Run seed {FormatSeed(seed)}");

        if (LogRolls)
            LogPreview();
    }

    /// <summary>
    /// Server only. The stream for one purpose of the run. Ask for it every time you draw instead of
    /// keeping it: a new run replaces every stream.
    /// </summary>
    public SeededRandom GetStream(RandomStream stream)
    {
        return GetOrCreate((int)stream);
    }

    /// <summary>
    /// Server only. The upgrade offers of one player, by slot (0 to 3): each player has their own,
    /// so what one picks never changes what the others are offered (catalog, rule 9).
    /// The slot and not the connection, because the connection id changes from run to run.
    /// </summary>
    public SeededRandom GetOfferStream(int slot)
    {
        if (slot < 0 || slot >= PlayerSlots.MaxPlayers)
        {
            Debug.LogError($"[RunRandomService] Slot {slot} doesn't exist. Using slot 0's offers.", this);
            slot = 0;
        }

        return GetOrCreate((int)RandomStream.Offers + slot);
    }

    /// <summary>
    /// How a seed is written for people: 8 hexadecimal characters, like 3A9F01C2.
    /// RunRandomSettings reads it back the same way.
    /// </summary>
    public static string FormatSeed(uint seed)
    {
        return seed.ToString("X8", CultureInfo.InvariantCulture);
    }

    private SeededRandom GetOrCreate(int id)
    {
        if (!HasRun)
        {
            // A client drawing, or something drawing before the host started. Both are bugs: a client's
            // numbers would differ from the host's. Starting a run keeps the game going.
            Debug.LogError("[RunRandomService] Asked for a random stream without a run. Only the host draws, " +
                           "and only while it hosts. Starting a run with a new seed.", this);
            BeginRun(PickSeed());
        }

        if (!_streams.TryGetValue(id, out SeededRandom random))
        {
            random = CreateStream(Seed, id);
            _streams.Add(id, random);
        }

        return random;
    }

    // Mix gives each stream its own scrambled starting point, and the stream number picks its PCG stream.
    // Either alone would do; both together keep streams of nearby seeds and numbers unrelated.
    private static SeededRandom CreateStream(uint seed, int id)
    {
        ulong start = SeededRandom.Mix(((ulong)seed << 32) | (uint)id);
        return new SeededRandom(start, (ulong)id);
    }

    private void OnServerStarted()
    {
        BeginRun(PickSeed());
    }

    // Leaving the match ends the run, so nothing draws from an old seed in the next one.
    private void OnClientConnectionState(SessionState state)
    {
        if (state != SessionState.Disconnected && state != SessionState.Lost && state != SessionState.Failed)
            return;

        Seed = 0;
        HasRun = false;
        _streams.Clear();
    }

    private uint PickSeed()
    {
        // Debug.isDebugBuild is also true in the editor.
        if (_settings != null && Debug.isDebugBuild)
        {
            if (_settings.TryGetFixedSeed(out uint fixedSeed))
                return fixedSeed;

            if (_settings.HasFixedSeedText)
                Debug.LogWarning($"[RunRandomService] The fixed seed of {_settings.name} isn't a valid seed " +
                                 "(8 hexadecimal characters, not all 0). Using a new one.", _settings);
        }

        return NewSeed();
    }

    // Not from UnityEngine.Random: its state is shared with the cosmetic effects. 0 is left out because
    // RunRandomSettings uses it as «no fixed seed», so every seed shown can be pasted back.
    private static uint NewSeed()
    {
        uint seed;
        do
        {
            seed = unchecked((uint)Guid.NewGuid().GetHashCode());
        } while (seed == 0);

        return seed;
    }

    // The first numbers of the fixed streams, from copies, so the real streams don't move.
    // The same seed must always print the same line (#75).
    private void LogPreview()
    {
        Debug.Log($"[RunRandomService] Seed {FormatSeed(Seed)} preview · " +
                  $"waves {Preview(RandomStream.Waves)} · rooms {Preview(RandomStream.Rooms)} · " +
                  $"offers J1 {Preview(RandomStream.Offers)}");
    }

    private string Preview(RandomStream stream)
    {
        SeededRandom copy = CreateStream(Seed, (int)stream);
        return $"{copy.Range(0, 100)} {copy.Range(0, 100)} {copy.Range(0, 100)}";
    }
}
