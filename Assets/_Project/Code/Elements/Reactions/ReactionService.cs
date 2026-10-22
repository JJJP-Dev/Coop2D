using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

// Runs each reaction's effects on the host: damage, clouds, chains, pushes and stuns, with the
// values of its ReactionDefinition. Enemies move on the host, so every client sees the result.
public class ReactionService : MonoBehaviour
{
    // The catalog's limit: a new cloud replaces the oldest one.
    private const int MaxClouds = 6;

    // A cloud renews its slow this often, for a little longer, so leaving it ends the slow.
    private const float CloudTickInterval = 0.25f;
    private const float CloudSlowDuration = 0.3f;

    [SerializeField] private NetworkManager _networkManager;

    [Tooltip("Only enemies on these layers are affected, so a reaction never hurts a teammate.")]
    [SerializeField] private LayerMask _enemyLayers;

    private readonly List<Collider2D> _colliders = new();
    private readonly List<Health> _enemies = new();
    private readonly List<Health> _chain = new();
    private readonly List<ReactionCloud> _clouds = new();
    private ContactFilter2D _enemyFilter;
    private float _nextCloudTick;

    private void Awake()
    {
        _enemyFilter = new ContactFilter2D();
        _enemyFilter.SetLayerMask(_enemyLayers);
        _enemyFilter.useTriggers = true;
    }

    private void Start()
    {
        if (_networkManager == null)
        {
            Debug.LogError("[ReactionService] NetworkManager is not assigned.", this);
            return;
        }

        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        _networkManager.ClientManager.RegisterBroadcast<ReactionChainBroadcast>(OnChainBroadcast);
    }

    private void OnDestroy()
    {
        if (_networkManager == null)
            return;

        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _networkManager.ClientManager.UnregisterBroadcast<ReactionChainBroadcast>(OnChainBroadcast);
    }

    /// <summary>
    /// Server only. AuraHolder calls it right after a reaction consumes an aura.
    /// The reaction's damage counts as the detonator's.
    /// </summary>
    public void Resolve(ReactionInfo reaction)
    {
        if (_networkManager == null || !_networkManager.IsServerStarted)
            return;

        ReactionDefinition definition = reaction.Reaction;
        NetworkObject detonator = reaction.Detonator;

        FindEnemiesAround(reaction, definition.DamageRadius);
        FindChain(reaction, definition);

        // Statuses go first: the damage below can kill and despawn some of these enemies.
        foreach (Health enemy in _enemies)
        {
            if (!enemy.TryGetComponent(out EnemyStatus status))
                continue;

            if (definition.StunDuration > 0f)
                status.ApplyStun(definition.StunDuration);

            if (definition.KnockbackDistance > 0f)
                status.ApplyPush(GetPushDirection(enemy, reaction), definition.KnockbackDistance, definition.KnockbackDuration);
        }

        if (definition.CloudDuration > 0f)
            AddCloud(reaction.Position, definition);

        if (_chain.Count > 0)
            SendChain(reaction.Position, definition.Color);

        foreach (Health enemy in _enemies)
            enemy.TryTakeDamage(definition.Damage, detonator, DamageKind.Reaction);

        foreach (Health link in _chain)
            link.TryTakeDamage(definition.ChainDamage, detonator, DamageKind.Reaction);
    }

    // With no radius, only the target. Otherwise every living enemy inside, the target included.
    private void FindEnemiesAround(ReactionInfo reaction, float radius)
    {
        _enemies.Clear();

        if (radius <= 0f)
        {
            if (reaction.Target != null && reaction.Target.TryGetComponent(out Health target) && !target.IsDead)
                _enemies.Add(target);

            return;
        }

        Physics2D.OverlapCircle(reaction.Position, radius, _enemyFilter, _colliders);

        foreach (Collider2D collider in _colliders)
        {
            Health health = collider.GetComponentInParent<Health>();
            if (health != null && !health.IsDead && !_enemies.Contains(health))
                _enemies.Add(health);
        }
    }

    // Each jump goes to the nearest enemy that carries the reaction's aura element
    // (wet, for Electrocution) within range of the last one hit. Collected before any damage,
    // while every enemy on the way is still alive.
    private void FindChain(ReactionInfo reaction, ReactionDefinition definition)
    {
        _chain.Clear();

        if (definition.ChainJumps <= 0 || reaction.Target == null)
            return;

        Health start = reaction.Target.GetComponent<Health>();
        Vector2 from = reaction.Position;

        for (int jump = 0; jump < definition.ChainJumps; jump++)
        {
            Health next = FindNearestLink(from, start, definition);
            if (next == null)
                break;

            _chain.Add(next);
            from = next.transform.position;
        }
    }

    private Health FindNearestLink(Vector2 from, Health start, ReactionDefinition definition)
    {
        Health nearest = null;
        float nearestSqrDistance = float.MaxValue;

        Physics2D.OverlapCircle(from, definition.ChainRange, _enemyFilter, _colliders);

        foreach (Collider2D collider in _colliders)
        {
            Health health = collider.GetComponentInParent<Health>();
            if (health == null || health == start || health.IsDead || _chain.Contains(health))
                continue;

            if (!health.TryGetComponent(out AuraHolder aura) || !aura.HasAura || aura.Element != definition.AuraElement)
                continue;

            float sqrDistance = ((Vector2)health.transform.position - from).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = health;
            }
        }

        return nearest;
    }

    // Every client draws it, the host's included. It goes after the reaction's RPC on the same
    // reliable channel, so it never arrives before the reaction it belongs to.
    private void SendChain(Vector2 start, Color color)
    {
        Vector2[] points = new Vector2[_chain.Count + 1];
        points[0] = start;

        for (int i = 0; i < _chain.Count; i++)
            points[i + 1] = _chain[i].transform.position;

        _networkManager.ServerManager.Broadcast(new ReactionChainBroadcast { Points = points, Color = color });
    }

    private void OnChainBroadcast(ReactionChainBroadcast broadcast, Channel channel)
    {
        if (broadcast.Points != null && broadcast.Points.Length > 1)
            GameServices.Instance.CombatEventService.RaiseChain(new ReactionChain(broadcast.Points, broadcast.Color));
    }

    // Straight out from the target. The target itself, at the center, moves away from the detonator.
    private static Vector2 GetPushDirection(Health enemy, ReactionInfo reaction)
    {
        Vector2 away = (Vector2)enemy.transform.position - reaction.Position;
        if (away.sqrMagnitude > 0.0025f)
            return away.normalized;

        if (reaction.Detonator != null)
        {
            Vector2 fromDetonator = reaction.Position - (Vector2)reaction.Detonator.transform.position;
            if (fromDetonator.sqrMagnitude > 0.0025f)
                return fromDetonator.normalized;
        }

        return Vector2.up;
    }

    private void AddCloud(Vector2 center, ReactionDefinition definition)
    {
        if (_clouds.Count >= MaxClouds)
            _clouds.RemoveAt(0);

        _clouds.Add(new ReactionCloud(center, definition.CloudRadius, definition.CloudSlowPercent / 100f, Time.time + definition.CloudDuration));

        // Its first slow lands on the next frame, not up to a tick later.
        _nextCloudTick = 0f;
    }

    private void Update()
    {
        if (_clouds.Count == 0 || Time.time < _nextCloudTick)
            return;

        _nextCloudTick = Time.time + CloudTickInterval;
        float now = Time.time;

        for (int i = _clouds.Count - 1; i >= 0; i--)
        {
            ReactionCloud cloud = _clouds[i];
            if (now >= cloud.EndsAt)
            {
                _clouds.RemoveAt(i);
                continue;
            }

            Physics2D.OverlapCircle(cloud.Center, cloud.Radius, _enemyFilter, _colliders);

            foreach (Collider2D collider in _colliders)
            {
                EnemyStatus status = collider.GetComponentInParent<EnemyStatus>();
                if (status != null)
                    status.ApplySlow(cloud.Slow, CloudSlowDuration);
            }
        }
    }

    /// <summary>
    /// Server only. Removes every cloud, for a restarted match (#52). They're only on the host:
    /// the clients drew theirs when the reaction happened and those fade by themselves.
    /// </summary>
    public void ClearClouds()
    {
        _clouds.Clear();
    }

    // Clouds belong to the match that made them.
    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
            _clouds.Clear();
    }
}
