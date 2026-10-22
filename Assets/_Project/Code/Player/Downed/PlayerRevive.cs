using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Gets this player up when it's downed and a living teammate stays close long enough (#51).
// The host decides everything: who is close, when the revive starts and when it ends. The clients
// only receive the tick it started on and work out the progress with their own copy of the tick,
// which FishNet keeps in step with the host's, like MatchClock. So the bar fills smoothly everywhere
// and nothing is sent while it fills.
[RequireComponent(typeof(PlayerDowned))]
public class PlayerRevive : NetworkBehaviour
{
    // Checking every frame would be wasted work: a tenth of a second is far below what players notice.
    private const float CheckInterval = 0.1f;

    // 0 means nobody is reviving: the host's tick is never 0 once the server runs.
    private const uint NotReviving = 0;

    [SerializeField] private ReviveSettings _settings;

    private readonly SyncVar<uint> _startTick = new();

    private Health _health;
    private PlayerDowned _downed;

    // Server side. The reviver is the closest living teammate at the last check.
    private float _nextCheckTime;
    private NetworkObject _reviver;

    /// <summary>
    /// From 0 to 1 while a teammate is reviving this player, on every client. 0 otherwise.
    /// </summary>
    public float Progress
    {
        get
        {
            if (!IsSpawned || _settings == null || _startTick.Value == NotReviving)
                return 0f;

            return Mathf.Clamp01((float)(TimeManager.TimePassed(_startTick.Value) / _settings.Duration));
        }
    }

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
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        float now = Time.time;
        if (now < _nextCheckTime)
            return;

        _nextCheckTime = now + CheckInterval;

        // Up again some other way, like the Heal All cheat.
        if (!_downed.IsDowned)
        {
            StopReviving();
            return;
        }

        _reviver = FindReviver();

        // Nobody close: the progress is lost (#51), so a revive is a choice to stay.
        if (_reviver == null)
        {
            StopReviving();
            return;
        }

        if (_startTick.Value == NotReviving)
        {
            _startTick.Value = TimeManager.Tick;
            return;
        }

        if (TimeManager.TimePassed(_startTick.Value) >= _settings.Duration)
            FinishRevive();
    }

    private void FinishRevive()
    {
        int health = Mathf.Max(1, Mathf.RoundToInt(_health.Max * _settings.HealthFraction));
        _health.Revive(health, _settings.InvulnerableDuration);
        StopReviving();
    }

    private void StopReviving()
    {
        _reviver = null;

        if (_startTick.Value != NotReviving)
            _startTick.Value = NotReviving;
    }

    // The nearest living teammate within the radius, or null.
    private NetworkObject FindReviver()
    {
        Vector2 position = transform.position;
        float radiusSqr = _settings.Radius * _settings.Radius;
        NetworkObject nearest = null;
        float nearestSqrDistance = float.MaxValue;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null || player == NetworkObject)
                continue;

            if (!player.TryGetComponent(out Health health) || health.IsDead)
                continue;

            float sqrDistance = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (sqrDistance <= radiusSqr && sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = player;
            }
        }

        return nearest;
    }
}
