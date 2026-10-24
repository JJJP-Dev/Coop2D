using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Gets this player up when it's downed and living teammates stay close long enough.
// The host decides everything: who is close, how fast it goes and when it ends. Each revive takes
// longer than the last one of the match, and every extra teammate close makes it faster.
// A teammate who takes a hit stops helping for a moment, so protecting whoever revives matters.
// The clients don't receive the progress itself but a line: where it was at a tick and how fast it
// grows from there. Each one works out the progress with its own copy of the tick, which FishNet
// keeps in step with the host's, like MatchClock. So the label fills smoothly everywhere, and the
// host only sends something when the speed changes: someone arrives, leaves or a revive ends.
// It also tells every client when this player goes down and gets up, for the launcher's events.
// Each revive uses one of the group's charges: with none left, nobody can get this player up.
[RequireComponent(typeof(PlayerDowned))]
public class PlayerRevive : NetworkBehaviour
{
    // Checking every frame would be wasted work: a tenth of a second is far below what players notice.
    private const float CheckInterval = 0.1f;

    [SerializeField] private ReviveSettings _settings;

    // The progress line. A rate of 0 means nobody is reviving. They change together, in the same tick,
    // so they arrive together; for a frame in between, only the label would be off.
    private readonly SyncVar<uint> _anchorTick = new();
    private readonly SyncVar<float> _anchorProgress = new();
    private readonly SyncVar<float> _rate = new();

    private Health _health;
    private PlayerDowned _downed;

    // Server side. The reviver is the closest teammate helping at the last check, for the launcher.
    private float _nextCheckTime;
    private NetworkObject _reviver;
    private bool _wasDowned;

    /// <summary>
    /// From 0 to 1 while teammates are reviving this player, on every client. 0 otherwise.
    /// </summary>
    public float Progress => IsSpawned ? ProgressNow() : 0f;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _downed = GetComponent<PlayerDowned>();

        if (_settings == null)
            Debug.LogError("[PlayerRevive] Revive settings are not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled players come back without the revive of their previous life.
        StopReviving();
        _nextCheckTime = 0f;
        _wasDowned = _downed.IsDowned;
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        float now = Time.time;
        if (now < _nextCheckTime)
            return;

        _nextCheckTime = now + CheckInterval;

        bool downed = _downed.IsDowned;
        if (downed != _wasDowned)
        {
            _wasDowned = downed;

            // Getting up without a revive is the Heal All cheat: nobody did it, and it uses no charge.
            if (downed)
                ObserversDowned();
            else
                ObserversRevived(null, ChargesLeft());
        }

        // Up again some other way, like the Heal All cheat.
        if (!downed)
        {
            StopReviving();
            return;
        }

        // No charges left: a teammate standing here changes nothing, so no progress shows.
        ReviveCharges charges = ReviveCharges.Current;
        if (charges != null && charges.Remaining <= 0)
        {
            StopReviving();
            return;
        }

        int helpers = CountHelpers(out _reviver);

        // Nobody helping: the progress is lost, so a revive is a choice to stay.
        if (helpers == 0)
        {
            StopReviving();
            return;
        }

        int used = charges != null ? charges.Used : 0;
        float rate = _settings.GetSpeed(helpers) / _settings.GetDuration(used);

        if (_rate.Value <= 0f)
        {
            SetLine(0f, rate);
            return;
        }

        float progress = ProgressNow();
        if (progress >= 1f)
        {
            FinishRevive();
            return;
        }

        // A new line from where it is now, so the progress never jumps, only bends.
        if (!Mathf.Approximately(rate, _rate.Value))
            SetLine(progress, rate);
    }

    /// <summary>
    /// Server only. Call it right after restoring this player's health for a restarted match:
    /// it forgets the revive and that the player was down, so getting up there isn't reported as a cheat.
    /// </summary>
    public void ResetForNewRun()
    {
        if (!IsServerInitialized)
            return;

        StopReviving();
        _wasDowned = _downed.IsDowned;
    }

    private void FinishRevive()
    {
        // Spent when it ends, not when it starts, so a revive cut short costs nothing. Two revives
        // ending at once with one charge left: the second finds none and stays down.
        ReviveCharges charges = ReviveCharges.Current;
        if (charges != null && !charges.TrySpend())
        {
            StopReviving();
            return;
        }

        int health = Mathf.Max(1, Mathf.RoundToInt(_health.Max * _settings.HealthFraction));
        _health.Revive(health, _settings.InvulnerableDuration);

        // Told here, with who did it, so the next check doesn't report it again as a cheat.
        _wasDowned = false;
        ObserversRevived(_reviver, ChargesLeft());

        StopReviving();
    }

    // Each client writes it from its own point of view. The host's client receives it too.
    [ObserversRpc]
    private void ObserversDowned()
    {
        if (TryGetComponent(out PlayerIdentity identity))
            LauncherEvents.PlayerDowned(identity.Slot + 1, IsOwner);
    }

    // Null reviver: nobody revived it (a cheat). A reviver that left the game also arrives as null.
    // The charges left travel with it: the charges' own value may reach a client a moment later.
    [ObserversRpc]
    private void ObserversRevived(NetworkObject reviver, int chargesLeft)
    {
        if (!TryGetComponent(out PlayerIdentity identity))
            return;

        string by = reviver == null ? LauncherEvents.ByNone
            : reviver.IsOwner ? LauncherEvents.ByMe
            : LauncherEvents.ByAlly;

        LauncherEvents.PlayerRevived(identity.Slot + 1, IsOwner, by, chargesLeft);
    }

    // -1 in a scene that doesn't limit revives.
    private static int ChargesLeft()
    {
        return ReviveCharges.Current != null ? ReviveCharges.Current.Remaining : -1;
    }

    // The same sum on the host and on every client.
    private float ProgressNow()
    {
        if (_rate.Value <= 0f)
            return 0f;

        return Mathf.Clamp01(_anchorProgress.Value + _rate.Value * (float)TimeManager.TimePassed(_anchorTick.Value));
    }

    // Server side.
    private void SetLine(float progress, float rate)
    {
        _anchorTick.Value = TimeManager.Tick;
        _anchorProgress.Value = progress;
        _rate.Value = rate;
    }

    private void StopReviving()
    {
        _reviver = null;

        if (_rate.Value <= 0f)
            return;

        _rate.Value = 0f;
        _anchorProgress.Value = 0f;
    }

    // Living teammates within the radius who weren't just hit, and the nearest of them,
    // which is null when there are none.
    private int CountHelpers(out NetworkObject nearest)
    {
        float hitBefore = Time.time - _settings.HitLockout;
        Vector2 position = transform.position;
        float radiusSqr = _settings.Radius * _settings.Radius;
        float nearestSqrDistance = float.MaxValue;
        int helpers = 0;
        nearest = null;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null || player == NetworkObject)
                continue;

            if (!player.TryGetComponent(out Health health) || health.IsDead)
                continue;

            // Hit a moment ago: it doesn't count until the lockout ends, while the others go on.
            if (health.LastDamagedAt > hitBefore)
                continue;

            float sqrDistance = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (sqrDistance > radiusSqr)
                continue;

            helpers++;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = player;
            }
        }

        return helpers;
    }
}
