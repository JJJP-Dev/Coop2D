using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

// Turns what the services already tell this client into launcher events: entering and leaving a match,
// other players coming and going, reactions and kills. It only listens: nothing here changes the game.
// Lives in Bootstrap, next to the services, so it lasts the whole session.
public class LauncherEventReporter : MonoBehaviour
{
    // A match that had been going this long when you arrived counts as a late join.
    private const int LateJoinSeconds = 10;

    [SerializeField] private NetworkManager _networkManager;

    private GameServices _services;

    private bool _inMatch;
    private int _matchJoinFrame = -1;
    private double _matchStartedAt;

    // Waves already written in this match, and whether waves_finished was.
    private int _reportedWaves;
    private bool _reportedWavesFinished;

    // Slots (1 to 4) of players who left this frame. Held until LateUpdate: when the whole session ends,
    // every player leaves at once, and that's a match_left, not a player_left for each one.
    private readonly List<int> _leftSlots = new();

    // Reactions this frame, grouped by variant, detonator, duo and this player's part in them.
    // One cast that triggers three equal Vapors is one line with targets=3.
    private readonly Dictionary<(string variant, int detonator, bool duo, string me), int> _reactions = new();

    private void Start()
    {
        _services = GameServices.Instance;

        if (_services == null || _networkManager == null)
        {
            Debug.LogError("[LauncherEventReporter] GameServices or NetworkManager is missing.", this);
            enabled = false;
            return;
        }

        _services.ConnectionService.ClientConnectionStateChanged += OnConnectionStateChanged;
        _services.LocalPlayerService.Changed += OnLocalPlayerChanged;
        _services.PlayerRegistry.Added += OnPlayerAdded;
        _services.PlayerRegistry.Removed += OnPlayerRemoved;
        _services.CombatEventService.ReactionTriggered += OnReactionTriggered;
        _services.CombatEventService.DeathConfirmed += OnDeathConfirmed;
    }

    private void OnDestroy()
    {
        if (_services == null)
            return;

        _services.ConnectionService.ClientConnectionStateChanged -= OnConnectionStateChanged;
        _services.LocalPlayerService.Changed -= OnLocalPlayerChanged;
        _services.PlayerRegistry.Added -= OnPlayerAdded;
        _services.PlayerRegistry.Removed -= OnPlayerRemoved;
        _services.CombatEventService.ReactionTriggered -= OnReactionTriggered;
        _services.CombatEventService.DeathConfirmed -= OnDeathConfirmed;
    }

    private void Update()
    {
        // One frame after our player appears: the other players who were already in the match
        // arrive in the same batch, in no fixed order, so by now they're all counted.
        if (_matchJoinFrame >= 0 && Time.frameCount > _matchJoinFrame)
            ReportMatchJoined();

        if (_inMatch)
            ReportWaves();
    }

    private void LateUpdate()
    {
        FlushLeftPlayers();
        FlushReactions();
    }

    // --- Match ---

    private void OnLocalPlayerChanged(PlayerIdentity player)
    {
        if (player != null && !_inMatch)
            _matchJoinFrame = Time.frameCount;
    }

    private void ReportMatchJoined()
    {
        _matchJoinFrame = -1;

        PlayerIdentity player = _services.LocalPlayerService.Current;
        if (player == null)
            return;

        bool isHost = _networkManager.IsServerStarted;
        int matchTime = isHost ? 0 : MatchTime();

        _inMatch = true;
        _matchStartedAt = Time.realtimeSinceStartupAsDouble;

        // The scene's seed arrived with the scene, before our player, so it's already here.
        string seed = MatchSeed.Current != null ? MatchSeed.Current.SeedText : null;
        LauncherEvents.MatchJoined(isHost, player.Slot + 1, _services.PlayerRegistry.Count, matchTime,
                                   !isHost && matchTime >= LateJoinSeconds, seed);

        // The host writes every wave, the ones that started before its own client connected included.
        // A client that arrives later only writes the waves that start from now on.
        WaveSpawner waves = WaveSpawner.Current;
        _reportedWaves = !isHost && waves != null ? waves.WavesStarted : 0;
        _reportedWavesFinished = !isHost && waves != null && waves.WavesFinished;

        // The element the slot gave us. It arrived with the player, so it's already here.
        // Changes with the test key are written by PlayerElement itself.
        if (player.TryGetComponent(out PlayerElement element))
            LauncherEvents.ElementSet(element.Current, "slot");
    }

    private void OnConnectionStateChanged(SessionState state)
    {
        if (state != SessionState.Disconnected && state != SessionState.Lost && state != SessionState.Failed)
            return;

        _matchJoinFrame = -1;
        _leftSlots.Clear();

        if (!_inMatch)
            return;

        _inMatch = false;

        // Lost is the host closing on purpose if it said so first (HostClosingBroadcast); otherwise the connection dropped.
        string reason = state == SessionState.Disconnected ? "quit"
            : _services.ConnectionService.HostClosedSession ? "host_closed"
            : "disconnected";
        LauncherEvents.MatchLeft(reason, (int)(Time.realtimeSinceStartupAsDouble - _matchStartedAt));
    }

    private void ReportWaves()
    {
        WaveSpawner waves = WaveSpawner.Current;
        if (waves == null)
            return;

        // The cheats panel started the schedule again.
        if (waves.WavesStarted < _reportedWaves)
            _reportedWaves = 0;

        if (!waves.WavesFinished)
            _reportedWavesFinished = false;

        while (_reportedWaves < waves.WavesStarted)
        {
            _reportedWaves++;
            LauncherEvents.WaveStarted(_reportedWaves, MatchTime());
        }

        // Alive means not downed (#51): players don't die, they go down and wait for a teammate.
        if (waves.WavesFinished && !_reportedWavesFinished)
        {
            _reportedWavesFinished = true;
            PlayerIdentity player = _services.LocalPlayerService.Current;
            bool alive = player == null || !player.TryGetComponent(out PlayerDowned downed) || !downed.IsDowned;
            LauncherEvents.WavesFinished(MatchTime(), alive);
        }
    }

    private static int MatchTime()
    {
        return MatchClock.Current != null ? MatchClock.Current.ElapsedSeconds : 0;
    }

    // --- Other players ---

    // Before our own match_joined they're simply part of the match we find.
    private void OnPlayerAdded(PlayerIdentity player)
    {
        if (_inMatch && !player.IsOwner)
            LauncherEvents.PlayerJoined(player.Slot + 1, _services.PlayerRegistry.Count);
    }

    private void OnPlayerRemoved(PlayerIdentity player)
    {
        if (_inMatch && !player.IsOwner)
            _leftSlots.Add(player.Slot + 1);
    }

    private void FlushLeftPlayers()
    {
        if (_leftSlots.Count == 0)
            return;

        // If our own player is gone too, the session ended: OnConnectionStateChanged reports that.
        if (_inMatch && _services.LocalPlayerService.Current != null && _networkManager.IsClientStarted)
        {
            foreach (int slot in _leftSlots)
                LauncherEvents.PlayerLeft(slot, _services.PlayerRegistry.Count);
        }

        _leftSlots.Clear();
    }

    // --- Combat ---

    private void OnReactionTriggered(ReactionInfo reaction)
    {
        bool auraIsMine = reaction.AuraOwner != null && reaction.AuraOwner.IsOwner;
        bool detonationIsMine = reaction.Detonator != null && reaction.Detonator.IsOwner;

        string me = auraIsMine && detonationIsMine ? LauncherEvents.RoleBoth
            : auraIsMine ? LauncherEvents.RoleAura
            : detonationIsMine ? LauncherEvents.RoleDetonator
            : LauncherEvents.RoleNone;

        int detonator = reaction.Detonator != null ? reaction.Detonator.ObjectId : -1;
        var key = (reaction.Reaction.VariantId, detonator, reaction.IsDuo, me);

        _reactions.TryGetValue(key, out int count);
        _reactions[key] = count + 1;
    }

    private void FlushReactions()
    {
        if (_reactions.Count == 0)
            return;

        foreach (KeyValuePair<(string variant, int detonator, bool duo, string me), int> group in _reactions)
            LauncherEvents.Reaction(group.Key.variant, group.Key.duo, group.Key.me, group.Value);

        _reactions.Clear();
    }

    // Only enemies and dummies: players can't die yet, and when they can it will be player_died.
    private void OnDeathConfirmed(HitInfo hit)
    {
        string enemy;

        if (hit.Target.TryGetComponent(out Enemy enemyComponent))
            enemy = enemyComponent.Definition != null && !string.IsNullOrEmpty(enemyComponent.Definition.Id)
                ? enemyComponent.Definition.Id
                : "unknown";
        else if (hit.Target.TryGetComponent(out TrainingDummy _))
            enemy = "dummy";
        else
            return;

        LauncherEvents.EnemyKilled(enemy, By(hit.Attacker), hit.Kind);
    }

    private static string By(NetworkObject attacker)
    {
        if (attacker == null)
            return LauncherEvents.ByNone;

        return attacker.IsOwner ? LauncherEvents.ByMe : LauncherEvents.ByAlly;
    }
}
