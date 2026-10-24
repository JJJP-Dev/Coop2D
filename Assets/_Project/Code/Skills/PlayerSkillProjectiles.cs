using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// The skills that fly, cast by PlayerSkills. They work like the basic attack: the owner flies its own at
// once and reports what it hits (Rayo) or where it bursts (Llamarada, Burbuja); every report goes on the
// same reliable channel as the launch, so the host always knows the launch first.
// The host remembers each accepted launch (SkillShotRecord) until it can no longer land, and only takes
// reports that fit it: a known launch, on its path, within its hits. The other PCs fly a copy that is
// only the picture.
[RequireComponent(typeof(PlayerSkills))]
public class PlayerSkillProjectiles : NetworkBehaviour
{
    // Extra seconds a projectile waits for its reports after it could have landed. The reports travel
    // with the same delay as the launch, so this only covers jitter and frozen frames.
    private const double ReportGrace = 1.0;

    private PlayerSkills _skills;

    private readonly Dictionary<ushort, SkillShotRecord> _pendingShots = new();
    private readonly List<ushort> _expiredShotIds = new();

    // Owner side: the id of the next projectile, so its reports name it.
    private ushort _nextCastId;

    private void Awake()
    {
        _skills = GetComponent<PlayerSkills>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled players come back with the launches of their previous life.
        _pendingShots.Clear();
    }

    /// <summary>
    /// Owner side, once PlayerSkills has started the cooldown. The host hears of the launch first, then
    /// our own projectile flies.
    /// </summary>
    public void Launch(AttackSlot slot, SkillInstance skill, Vector2 origin, Vector2 point, uint tick)
    {
        ushort castId = _nextCastId;
        _nextCastId = unchecked((ushort)(_nextCastId + 1));

        if (IsServerInitialized)
            LaunchOnServer(slot, origin, point, castId, tick);
        else
            ServerLaunch(slot, origin, point, castId, tick);

        SkillProjectile projectile = SpawnProjectile(skill, origin);
        if (projectile == null)
            return;

        Color color = _skills.GetSkillColor(skill);
        if (skill.Bursts)
        {
            projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius, 1,
                              _skills.Hits.TargetLayers, color, 0f,
                              onStop: (stopPoint, hit) => ReportBurst(castId, skill, stopPoint, hit));
        }
        else
        {
            projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius,
                              1 + skill.Pierce, _skills.Hits.TargetLayers, color, 0f,
                              onHit: hit => ReportProjectileHit(castId, hit));
        }
    }

    // On the caster's PC, each enemy its own projectile hits directly.
    private void ReportProjectileHit(ushort castId, Collider2D hit)
    {
        // The projectile can outlive its caster.
        if (this == null || !IsSpawned)
            return;

        Health target = hit.GetComponentInParent<Health>();
        if (target == null || target.IsDead)
            return;

        if (IsServerInitialized)
            ApplyProjectileHit(castId, target);
        else
            ServerProjectileHit(castId, target);
    }

    // On the caster's PC, where its own projectile stopped: it bursts there, finds what the circle touches
    // on this screen and draws it at once, as an instant skill does.
    private void ReportBurst(ushort castId, SkillInstance skill, Vector2 point, Collider2D hit)
    {
        if (this == null || !IsSpawned)
            return;

        // It reached the end of its range without touching anyone, and this skill only bursts on contact.
        if (hit == null && !skill.BurstAtEnd)
            return;

        SkillArea area = SkillArea.Circle(point, skill.ImpactRadius);
        List<Health> targets = _skills.Hits.FindTargets(area);
        _skills.ShowCast(skill, area);

        // The enemy it touched is the one a trapping skill traps.
        Health touched = hit != null ? hit.GetComponentInParent<Health>() : null;

        if (IsServerInitialized)
            ApplyBurst(castId, point, targets, touched);
        else
            ServerBurst(castId, point, targets, touched);
    }

    [ServerRpc]
    private void ServerLaunch(AttackSlot slot, Vector2 origin, Vector2 point, ushort castId, uint tick)
    {
        LaunchOnServer(slot, origin, point, castId, tick);
    }

    private void LaunchOnServer(AttackSlot slot, Vector2 origin, Vector2 point, ushort castId, uint tick)
    {
        if (!_skills.TryAcceptCast(slot, origin, point, true, out SkillInstance skill))
            return;

        ForgetExpiredShots();

        // Remembered until it could no longer land, plus the time its reports take to arrive.
        SkillArea path = SkillArea.For(skill, origin, point);
        double expiresAt = Time.realtimeSinceStartupAsDouble + skill.FlightTime + ReportGrace;
        _pendingShots[castId] = new SkillShotRecord(slot, skill, path, expiresAt, skill.Bursts ? 1 : 1 + skill.Pierce);

        _skills.RaiseCastAccepted(tick);

        // The caster already flies its own; the host's copy of a client's projectile is only the picture.
        if (!IsOwner)
            LaunchCopy(skill, origin, point, (float)TimeManager.TimePassed(tick));

        ObserversLaunch(slot, origin, point, tick);
    }

    // Copies start late by the time the launch took to arrive, and catch up with the caster's.
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversLaunch(AttackSlot slot, Vector2 origin, Vector2 point, uint tick)
    {
        SkillInstance skill = _skills.GetSkill(slot);
        if (skill != null && skill.IsProjectile)
            LaunchCopy(skill, origin, point, (float)TimeManager.TimePassed(tick));
    }

    private void LaunchCopy(SkillInstance skill, Vector2 origin, Vector2 point, float timePassed)
    {
        SkillProjectile projectile = SpawnProjectile(skill, origin);
        if (projectile == null)
            return;

        int hits = skill.Bursts ? 1 : 1 + skill.Pierce;
        projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius, hits,
                          _skills.Hits.TargetLayers, _skills.GetSkillColor(skill), timePassed);
    }

    // A target that no longer exists arrives as null.
    [ServerRpc]
    private void ServerProjectileHit(ushort castId, Health target)
    {
        ApplyProjectileHit(castId, target);
    }

    private void ApplyProjectileHit(ushort castId, Health target)
    {
        if (!IsServerInitialized || !TryGetShot(castId, false, out SkillShotRecord shot))
            return;

        // Each enemy is hit once per projectile.
        if (target == null || shot.Hit.Contains(target))
            return;

        if (!_skills.Hits.TryHitAlong(shot.Skill, shot.Slot, shot.Path, target))
            return;

        shot.Hit.Add(target);
        shot.HitsLeft--;
        if (shot.HitsLeft <= 0)
            _pendingShots.Remove(castId);
    }

    [ServerRpc]
    private void ServerBurst(ushort castId, Vector2 point, List<Health> targets, Health touched)
    {
        ApplyBurst(castId, point, targets, touched);
    }

    private void ApplyBurst(ushort castId, Vector2 point, List<Health> targets, Health touched)
    {
        if (!IsServerInitialized || !TryGetShot(castId, true, out SkillShotRecord shot))
            return;

        // One burst per launch.
        _pendingShots.Remove(castId);

        // It can only burst somewhere on its path.
        float distance = shot.Path.DistanceOutside(point);
        if (distance > _skills.Hits.TargetTolerance)
        {
            Debug.LogWarning($"[PlayerSkillProjectiles] Rejected the burst of {shot.Skill.Id} from client {OwnerId}: {distance:0.00} units from its path.", this);
            return;
        }

        SkillArea area = SkillArea.Circle(point, shot.Skill.ImpactRadius);
        _skills.Hits.ApplyArea(shot.Skill, shot.Slot, area, targets, touched);

        // The caster already drew it, so the host only draws a client's burst.
        if (!IsOwner)
            _skills.ShowCast(shot.Skill, area);

        ObserversBurst(shot.Slot, point);
    }

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversBurst(AttackSlot slot, Vector2 point)
    {
        SkillInstance skill = _skills.GetSkill(slot);
        if (skill != null && skill.Bursts)
            _skills.ShowCast(skill, SkillArea.Circle(point, skill.ImpactRadius));
    }

    // Unknown (never accepted, already used up or forgotten) or too old: the report doesn't count.
    private bool TryGetShot(ushort castId, bool bursts, out SkillShotRecord shot)
    {
        if (_pendingShots.TryGetValue(castId, out shot) && Time.realtimeSinceStartupAsDouble <= shot.ExpiresAt &&
            shot.Skill.Bursts == bursts)
            return true;

        Debug.LogWarning($"[PlayerSkillProjectiles] Rejected a projectile report from client {OwnerId}: launch {castId} is unknown, used or too old.", this);
        shot = null;
        return false;
    }

    // Projectiles that land on nothing never report, so they are dropped once they can no longer.
    private void ForgetExpiredShots()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        foreach (KeyValuePair<ushort, SkillShotRecord> pair in _pendingShots)
        {
            if (now > pair.Value.ExpiresAt)
                _expiredShotIds.Add(pair.Key);
        }

        foreach (ushort castId in _expiredShotIds)
            _pendingShots.Remove(castId);

        _expiredShotIds.Clear();
    }

    private SkillProjectile SpawnProjectile(SkillInstance skill, Vector2 origin)
    {
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[PlayerSkillProjectiles] {skill.Id} is a projectile without a Projectile Prefab.", skill.Definition);
            return null;
        }

        return Instantiate(skill.ProjectilePrefab, origin, Quaternion.identity);
    }
}
