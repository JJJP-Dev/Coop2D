using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Every experience orb on the ground in this match (#84). The host drops them when enemies die and
// decides who picks each one up, so no orb is ever picked up twice; the clients only draw them.
// They aren't NetworkObjects: one synced list with id, position and value. Dropping, merging or picking
// an orb sends a small change, and a player who arrives late gets the whole list with the scene.
// One per match scene, next to RunExperience.
public class ExperienceOrbs : NetworkBehaviour
{
    // A tenth of a second is far below what players notice, and 40 orbs × 4 players is nothing.
    private const float PickupInterval = 0.1f;

    [SerializeField] private OrbSettings _settings;

    [Tooltip("Where picked orbs go.")]
    [SerializeField] private RunExperience _experience;

    private readonly SyncList<ExperienceOrb> _orbs = new();

    // Server side.
    private ushort _nextId;
    private float _nextPickupTime;

    /// <summary>
    /// The orbs of the match this PC is in, on the host and on every client. Null outside a match scene.
    /// </summary>
    public static ExperienceOrbs Current { get; private set; }

    public OrbSettings Settings => _settings;

    // Client side, the host's client included, for ExperienceOrbsView. Added is also raised for the
    // orbs already on the ground when this client arrives, so the view can draw them.
    public event Action<ExperienceOrb> OrbAdded;
    public event Action<ExperienceOrb> OrbChanged;
    public event Action<ushort> OrbRemoved;
    public event Action OrbsCleared;

    /// <summary>
    /// Client side. An orb was picked up: where it was and who took it (null if they already left).
    /// </summary>
    public event Action<ushort, Vector2, NetworkObject> OrbPicked;

    private void Awake()
    {
        _orbs.OnChange += OnOrbsChanged;

        if (_settings == null || _experience == null)
            Debug.LogError("[ExperienceOrbs] Orb settings or experience are not assigned.", this);
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

        _orbs.Clear();
        _nextId = 0;
        _nextPickupTime = 0f;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // The client's copy of the list: on the host it's the one the host's client has received.
        foreach (ExperienceOrb orb in _orbs.GetCollection(false))
            OrbAdded?.Invoke(orb);
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null || _orbs.Count == 0)
            return;

        if (Time.time < _nextPickupTime)
            return;

        _nextPickupTime = Time.time + PickupInterval;
        PickUpOrbs();
    }

    /// <summary>
    /// Server only. An orb worth this much experience at this point. Beyond the limit, its value goes
    /// to the nearest orb instead.
    /// </summary>
    public void Drop(Vector2 position, int value)
    {
        if (!IsServerInitialized || _settings == null || value <= 0)
            return;

        if (_orbs.Count >= _settings.MaxOrbs)
        {
            int nearest = FindNearestOrb(position);
            ExperienceOrb merged = _orbs[nearest];
            merged.Value += value;
            _orbs[nearest] = merged;
            return;
        }

        _orbs.Add(new ExperienceOrb(_nextId++, position, value));
    }

    /// <summary>
    /// Server only. No orbs on the ground, for a restarted match (#52).
    /// </summary>
    public void ResetForNewRun()
    {
        if (IsServerInitialized)
            _orbs.Clear();
    }

    // Each orb goes to the nearest standing player within reach. Downed players don't pick up:
    // they can't move to them either.
    private void PickUpOrbs()
    {
        float radiusSqr = _settings.PickupRadius * _settings.PickupRadius;

        // Backwards, so removing one doesn't skip the next.
        for (int i = _orbs.Count - 1; i >= 0; i--)
        {
            ExperienceOrb orb = _orbs[i];
            NetworkObject picker = FindNearestStandingPlayer(orb.Position, radiusSqr);
            if (picker == null)
                continue;

            // Sent before removing it, on the same reliable channel as the list, so the clients
            // know who took it when they see it go.
            ObserversPicked(orb.Id, orb.Position, picker);
            _orbs.RemoveAt(i);
            _experience.AddExperience(orb.Value);
        }
    }

    [ObserversRpc]
    private void ObserversPicked(ushort id, Vector2 position, NetworkObject picker)
    {
        OrbPicked?.Invoke(id, position, picker);
    }

    // The view hears the client's copy of the list. The host's client gets its own calls with asServer false.
    private void OnOrbsChanged(SyncListOperation operation, int index, ExperienceOrb previous, ExperienceOrb next, bool asServer)
    {
        if (asServer)
            return;

        switch (operation)
        {
            case SyncListOperation.Add:
            case SyncListOperation.Insert:
                OrbAdded?.Invoke(next);
                break;
            case SyncListOperation.Set:
                OrbChanged?.Invoke(next);
                break;
            case SyncListOperation.RemoveAt:
                OrbRemoved?.Invoke(previous.Id);
                break;
            case SyncListOperation.Clear:
                OrbsCleared?.Invoke();
                break;
        }
    }

    private int FindNearestOrb(Vector2 position)
    {
        int nearest = 0;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < _orbs.Count; i++)
        {
            float sqrDistance = (_orbs[i].Position - position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = i;
            }
        }

        return nearest;
    }

    private NetworkObject FindNearestStandingPlayer(Vector2 position, float radiusSqr)
    {
        NetworkObject nearest = null;
        float nearestSqrDistance = float.MaxValue;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null || !player.TryGetComponent(out Health health) || health.IsDead)
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
