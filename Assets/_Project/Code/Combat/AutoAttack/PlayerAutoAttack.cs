using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlayerAutoAttack : NetworkBehaviour
{
    // Shots the server lets a player save up, so network jitter or a frozen frame
    // never rejects an honest shot.
    private const float MaxShotBudget = 3f;

    // Extra seconds a shot waits for its hit report. The report travels with the same delay
    // as the shot, so this only has to cover network jitter and frozen frames.
    private const float HitReportGrace = 1f;

    [SerializeField] private AutoAttackSettings _settings;

    private readonly List<Collider2D> _candidates = new();
    private ContactFilter2D _targetFilter;
    private PlayerElement _element;

    // Owner side.
    private float _nextAttackTime;
    private ushort _nextShotId;

    // Server side.
    private float _shotBudget;
    private double _lastBudgetTime;
    private readonly Dictionary<ushort, ShotRecord> _pendingShots = new();
    private readonly List<ushort> _expiredShotIds = new();

    private void Awake()
    {
        if (!TryGetComponent(out _element))
            Debug.LogError("[PlayerAutoAttack] No PlayerElement on this player, so its shots leave no aura.", this);

        if (_settings == null || _settings.ProjectilePrefab == null)
        {
            Debug.LogError("[PlayerAutoAttack] Settings or their projectile prefab are not assigned.", this);
            return;
        }

        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(_settings.TargetLayers);
        _targetFilter.useTriggers = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _shotBudget = 1f;
        _lastBudgetTime = Time.realtimeSinceStartupAsDouble;
        _pendingShots.Clear();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner && _settings != null)
            _nextAttackTime = Time.time + _settings.AttackInterval;
    }

    private void Update()
    {
        // Each player decides their own shots, with what they see on their own screen.
        if (!IsOwner || _settings == null || _settings.ProjectilePrefab == null)
            return;

        if (Time.time < _nextAttackTime)
            return;

        Vector2 origin = GetFireOrigin();

        // With nothing in range the attack stays ready and fires as soon as a target appears.
        if (!TryFindNearestTarget(origin, out Collider2D target))
            return;

        _nextAttackTime = Time.time + _settings.AttackInterval;
        Fire(origin, (GetAimPoint(origin, target) - origin).normalized);
    }

    // Ahead of a moving target, so the shot meets it instead of passing behind it.
    // The shot still flies straight, which is what the host checks hits against.
    private Vector2 GetAimPoint(Vector2 origin, Collider2D target)
    {
        // Aim at the middle of the body, not at its feet.
        Vector2 position = target.bounds.center;

        MotionTracker motion = target.GetComponentInParent<MotionTracker>();
        if (motion == null || _settings.LeadFactor <= 0f)
            return position;

        ProjectileSettings projectile = _settings.ProjectilePrefab.Settings;
        Vector2 intercept = AimPrediction.GetInterceptPoint(origin, position, motion.Velocity, projectile.Speed, projectile.Lifetime);
        return Vector2.Lerp(position, intercept, _settings.LeadFactor);
    }

    private void Fire(Vector2 origin, Vector2 direction)
    {
        uint tick = TimeManager.Tick;
        ushort shotId = _nextShotId++;

        if (IsServerInitialized)
        {
            // The host's own player: the server's copy is the one the host sees.
            FireOnServer(origin, direction, tick, shotId);
            return;
        }

        ServerFire(origin, direction, tick, shotId);
        SpawnProjectile(origin).Launch(direction, 0f, hit => ReportHit(shotId, hit));
    }

    [ServerRpc]
    private void ServerFire(Vector2 origin, Vector2 direction, uint tick, ushort shotId)
    {
        FireOnServer(origin, direction, tick, shotId);
    }

    private void FireOnServer(Vector2 origin, Vector2 direction, uint tick, ushort shotId)
    {
        if (direction.sqrMagnitude < 0.01f || !TrySpendShot())
        {
            Debug.LogWarning($"[PlayerAutoAttack] Rejected a shot from client {OwnerId}: too fast or without a direction.", this);
            return;
        }

        direction.Normalize();

        // The host sees this player slightly in the past, so it allows some distance.
        // Beyond that it doesn't reject the shot: it fires it from where it sees the player.
        Vector2 expectedOrigin = GetFireOrigin();
        float tolerance = _settings.OriginTolerance;
        if ((origin - expectedOrigin).sqrMagnitude > tolerance * tolerance)
            origin = expectedOrigin;

        RememberShot(shotId, origin, direction);

        float timePassed = (float)TimeManager.TimePassed(tick);
        Projectile projectile = SpawnProjectile(origin);

        // The host's own projectile applies its hits. A client reports its own,
        // so the host's copy of a client's shot is only the picture.
        if (IsOwner)
            projectile.Launch(direction, timePassed, hit => ApplyHit(shotId, hit.GetComponentInParent<Health>()));
        else
            projectile.Launch(direction, timePassed);

        ObserversFire(origin, direction, tick);
    }

    // The shooter and the host already show their own copies.
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversFire(Vector2 origin, Vector2 direction, uint tick)
    {
        float timePassed = (float)TimeManager.TimePassed(tick);
        SpawnProjectile(origin).Launch(direction, timePassed);
    }

    // On the shooter's client, when its own projectile hits something.
    private void ReportHit(ushort shotId, Collider2D hit)
    {
        // The projectile can outlive its shooter.
        if (!IsClientInitialized)
            return;

        Health target = hit.GetComponentInParent<Health>();
        if (target != null && !target.IsDead)
            ServerHit(shotId, target);
    }

    // It goes after ServerFire on the same reliable channel, so the host always knows the shot first.
    [ServerRpc]
    private void ServerHit(ushort shotId, Health target)
    {
        ApplyHit(shotId, target);
    }

    private void ApplyHit(ushort shotId, Health target)
    {
        // The projectile can outlive its shooter.
        if (!IsServerInitialized)
            return;

        // Each shot hits once, and only while its projectile could still be flying.
        if (!_pendingShots.TryGetValue(shotId, out ShotRecord shot) || Time.time > shot.ExpiresAt)
        {
            Debug.LogWarning($"[PlayerAutoAttack] Rejected a hit from client {OwnerId}: shot {shotId} is unknown, used or too old.", this);
            return;
        }

        _pendingShots.Remove(shotId);

        // Someone else may have killed it while the report was on its way.
        if (target == null || target.IsDead)
            return;

        // Only enemies, so a wrong report can never hurt a teammate.
        if (!target.TryGetComponent(out Collider2D body) || (_settings.TargetLayers.value & (1 << body.gameObject.layer)) == 0)
        {
            Debug.LogWarning($"[PlayerAutoAttack] Rejected a hit from client {OwnerId}: {target.name} has no collider on the target layers.", this);
            return;
        }

        float distance = DistanceFromPath(shot, body);
        if (distance > _settings.HitTolerance)
        {
            Debug.LogWarning($"[PlayerAutoAttack] Rejected a hit from client {OwnerId} on {target.name}: {distance:0.00} units from the shot's path.", this);
            return;
        }

        target.TryTakeDamage(_settings.Damage, NetworkObject);

        // The basic attack only marks: a weak aura of the shooter's element, never a reaction.
        // A killing hit has despawned the enemy already, so it gets no aura.
        if (!target.IsDead && _element != null && target.TryGetComponent(out AuraHolder aura))
            aura.TryApply(_element.Current, AuraStrength.Weak, NetworkObject, AttackSlot.Basic, canDetonate: false);
    }

    private void RememberShot(ushort shotId, Vector2 origin, Vector2 direction)
    {
        ForgetExpiredShots();

        ProjectileSettings projectile = _settings.ProjectilePrefab.Settings;
        Vector2 end = origin + direction * (projectile.Speed * projectile.Lifetime);
        float expiresAt = Time.time + projectile.Lifetime + HitReportGrace;
        _pendingShots[shotId] = new ShotRecord(origin, end, expiresAt);
    }

    // Shots that miss never get a report, so they are dropped once they can no longer hit.
    private void ForgetExpiredShots()
    {
        float now = Time.time;
        foreach (KeyValuePair<ushort, ShotRecord> pair in _pendingShots)
        {
            if (now > pair.Value.ExpiresAt)
                _expiredShotIds.Add(pair.Key);
        }

        foreach (ushort shotId in _expiredShotIds)
            _pendingShots.Remove(shotId);

        _expiredShotIds.Clear();
    }

    // Measured to the edge of the body rather than its center, so its size barely matters.
    private static float DistanceFromPath(ShotRecord shot, Collider2D body)
    {
        Vector2 path = shot.End - shot.Start;
        Vector2 center = body.bounds.center;
        float along = Mathf.Clamp01(Vector2.Dot(center - shot.Start, path) / path.sqrMagnitude);
        Vector2 nearestOnPath = shot.Start + path * along;
        return Vector2.Distance(nearestOnPath, body.ClosestPoint(nearestOnPath));
    }

    // One shot comes back every attack interval, up to MaxShotBudget. It counts real time:
    // Time.time skips part of a long frame, and the shots waiting behind it would find no budget.
    private bool TrySpendShot()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        _shotBudget = Mathf.Min(MaxShotBudget, _shotBudget + (float)(now - _lastBudgetTime) / _settings.AttackInterval);
        _lastBudgetTime = now;

        if (_shotBudget < 1f)
            return false;

        _shotBudget -= 1f;
        return true;
    }

    private Vector2 GetFireOrigin()
    {
        return (Vector2)transform.position + _settings.FireOffset;
    }

    private Projectile SpawnProjectile(Vector2 origin)
    {
        return Instantiate(_settings.ProjectilePrefab, origin, Quaternion.identity);
    }

    private bool TryFindNearestTarget(Vector2 origin, out Collider2D target)
    {
        target = null;
        float nearestSqrDistance = float.MaxValue;

        Physics2D.OverlapCircle(origin, _settings.Range, _targetFilter, _candidates);

        foreach (Collider2D candidate in _candidates)
        {
            Health health = candidate.GetComponentInParent<Health>();
            if (health == null || health.IsDead)
                continue;

            float sqrDistance = ((Vector2)candidate.bounds.center - origin).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                target = candidate;
            }
        }

        return target != null;
    }
}
