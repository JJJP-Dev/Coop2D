using System.Collections.Generic;
using System.Text;
using FishNet.Object;
using UnityEngine;

// The upgrade offers of the match in this scene (#78). On the host it hands every player an offer for
// each level up of the group, draws them with each player's own stream of the seed and gives the
// timed rerolls; each player's PlayerUpgrades keeps their queue and talks to its owner.
// On the clients it only lends the pool, to read the ids that arrive. One per match scene, next to
// RunExperience. Without it in a scene (ReactionLab) nobody gets offers.
public class UpgradeOffers : NetworkBehaviour
{
    // The timed rerolls are minutes apart: twice a second is plenty.
    private const float RerollCheckInterval = 0.5f;

    [SerializeField] private UpgradePool _pool;
    [SerializeField] private OfferSettings _settings;

    [Tooltip("Its level ups are what create the offers.")]
    [SerializeField] private RunExperience _experience;

    [Tooltip("Optional. The timed rerolls of the Pre 3 count its time; without it there are none.")]
    [SerializeField] private MatchClock _clock;

    // Server side.
    private readonly List<PlayerUpgrades> _players = new();
    private OfferGenerator _generator;
    private int _timedRerollsGiven;
    private float _nextRerollCheck;

    /// <summary>
    /// The offers of the match this PC is in, on the host and on every client. Null outside a match scene.
    /// </summary>
    public static UpgradeOffers Current { get; private set; }

    public UpgradePool Pool => _pool;
    public OfferSettings Settings => _settings;

    private void Awake()
    {
        if (_pool == null || _settings == null || _experience == null)
        {
            Debug.LogError("[UpgradeOffers] Pool, settings or experience are not assigned.", this);
            return;
        }

        _generator = new OfferGenerator(_pool, _settings);
    }

    // OnStartNetwork runs once on the host, so Current is there for its server code and for its client.
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Current = this;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (Current == this)
            Current = null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _timedRerollsGiven = 0;
        _nextRerollCheck = 0f;

        if (_experience != null)
            _experience.LeveledUp += OnLeveledUp;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (_experience != null)
            _experience.LeveledUp -= OnLeveledUp;

        _players.Clear();
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null || _clock == null || Time.time < _nextRerollCheck)
            return;

        _nextRerollCheck = Time.time + RerollCheckInterval;
        GiveTimedRerolls();
    }

    /// <summary>
    /// Server only. A player arrived: they start like everyone did and get an offer for every level the
    /// group already went through, so joining late doesn't leave them with fewer upgrades.
    /// </summary>
    public void Register(PlayerUpgrades player)
    {
        if (!IsServerInitialized || player == null || _players.Contains(player) || _settings == null)
            return;

        _players.Add(player);
        player.ResetForNewRun(_settings.Banishes);
        player.EarnRerollThirds(3 * _timedRerollsGiven, _settings.MaxRerollThirds);

        int level = _experience != null ? _experience.Level : 1;
        for (int missed = 2; missed <= level; missed++)
            player.EnqueueOffer(missed);
    }

    /// <summary>
    /// Server only.
    /// </summary>
    public void Unregister(PlayerUpgrades player)
    {
        _players.Remove(player);
    }

    /// <summary>
    /// Server only. Every player back to no upgrades and no offers, for a restarted match (#52).
    /// </summary>
    public void ResetForNewRun()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        _timedRerollsGiven = 0;

        foreach (PlayerUpgrades player in _players)
            player.ResetForNewRun(_settings.Banishes);
    }

    /// <summary>
    /// Server only. A whole reroll for every player, within the match's limit. For the cheats panel.
    /// </summary>
    public void GiveRerollToAll()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        foreach (PlayerUpgrades player in _players)
            player.EarnRerollThirds(3, _settings.MaxRerollThirds);
    }

    /// <summary>
    /// Server only. A whole new offer of this level for this player. For a reroll, options holds the
    /// offer being rerolled, and its evolution stays.
    /// </summary>
    public void Generate(PlayerUpgrades player, int level, List<OfferOption> options, string why)
    {
        if (_generator == null)
        {
            options.Clear();
            return;
        }

        OfferContext context = CreateContext(player, level);
        _generator.Generate(context, GetStream(player), options);
        player.CountOffer(_generator.CouldOfferDuo(context), HasDuo(options));
        LogOffer(player, level, options, why);
    }

    /// <summary>
    /// Server only. Another option in the place of the one at index, for a banish or an option that stopped
    /// being valid. False when it can't be replaced.
    /// </summary>
    public bool Replace(PlayerUpgrades player, int level, List<OfferOption> options, int index, string why)
    {
        if (_generator == null)
            return false;

        bool replaced = _generator.Replace(CreateContext(player, level), GetStream(player), options, index);
        LogOffer(player, level, options, why);
        return replaced;
    }

    /// <summary>
    /// Server only. Whether the upgrade is still valid for this player, checked again when they pick it.
    /// </summary>
    public bool IsStillOfferable(PlayerUpgrades player, int level, UpgradeDefinition upgrade)
    {
        return _generator != null && _generator.IsStillOfferable(CreateContext(player, level), upgrade);
    }

    private void OnLeveledUp(int level)
    {
        // Downed players too: choosing gives them something to do while they wait (catalog B.4).
        foreach (PlayerUpgrades player in _players)
            player.EnqueueOffer(level);
    }

    // +1 reroll for everyone at 4 and 8 minutes, only while the match is being played (catalog G.1).
    // They're counted, so someone joining later gets the ones already given.
    private void GiveTimedRerolls()
    {
        int[] times = _settings.TimedRerollSeconds;
        if (times == null || _timedRerollsGiven >= times.Length)
            return;

        if (MatchFlow.Current != null && MatchFlow.Current.Phase != MatchPhase.Playing)
            return;

        int elapsed = _clock.ElapsedSeconds;
        while (_timedRerollsGiven < times.Length && elapsed >= times[_timedRerollsGiven])
        {
            _timedRerollsGiven++;

            foreach (PlayerUpgrades player in _players)
                player.EarnRerollThirds(3, _settings.MaxRerollThirds);

            Debug.Log($"[UpgradeOffers] Timed reroll {_timedRerollsGiven} at {elapsed} s for every player.", this);
        }
    }

    private OfferContext CreateContext(PlayerUpgrades player, int level)
    {
        return new OfferContext(level, Mathf.Max(1, _players.Count), player, upgrade => IsTakenByOther(upgrade, player));
    }

    private bool IsTakenByOther(UpgradeDefinition upgrade, PlayerUpgrades player)
    {
        foreach (PlayerUpgrades other in _players)
        {
            if (other != player && other.GetStacks(upgrade.Id) > 0)
                return true;
        }

        return false;
    }

    // Each player's own stream (#75): what one does never changes what another is offered.
    private static SeededRandom GetStream(PlayerUpgrades player)
    {
        return GameServices.Instance.RunRandomService.GetOfferStream(player.Slot);
    }

    private static bool HasDuo(List<OfferOption> options)
    {
        foreach (OfferOption option in options)
        {
            if (option.Place == OfferPlace.Duo)
                return true;
        }

        return false;
    }

    // With Log Rolls on (RunRandomSettings), one line per offer, to compare two runs of the same seed.
    private static void LogOffer(PlayerUpgrades player, int level, List<OfferOption> options, string why)
    {
        if (!GameServices.Instance.RunRandomService.LogRolls)
            return;

        StringBuilder line = new();
        line.Append("[UpgradeOffers] J").Append(player.Slot + 1).Append(" nivel ").Append(level)
            .Append(" (").Append(why).Append("): ");

        for (int i = 0; i < options.Count; i++)
        {
            if (i > 0)
                line.Append(", ");

            line.Append(options[i].Upgrade != null ? options[i].Upgrade.Id : "null");
        }

        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", line.ToString());
    }
}
