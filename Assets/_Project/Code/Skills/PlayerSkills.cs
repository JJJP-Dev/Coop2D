using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;

// The player's skills. The owner aims and casts at once, with what it sees on its screen.
// The host checks the cast and applies its effects, and the other clients draw it.
// Which skills: the starting ones of the player's character (#162). The character arrives with the
// player's spawn, so every PC reads the same ones without sending them.
// Where they hit: a circle, a line or a cone (SkillArea). The owner sends where it stood and where it
// aimed, and the host rebuilds the same area from those two points to check the targets.
// Projectiles (#162) work like the basic attack: the owner flies its own and reports what it hits or
// where it bursts; the host remembers each accepted launch (SkillShotRecord) to check those reports.
[RequireComponent(typeof(PlayerElement))]
[RequireComponent(typeof(PlayerCharacter))]
public class PlayerSkills : NetworkBehaviour
{
    public const int SlotCount = 3;

    // A line or a cone needs a direction: closer than this, the cursor is on the player and gives none.
    private const float MinAimDistance = 0.1f;

    // A circle's aim point is clamped to its range on the owner; this only absorbs rounding.
    private const float AimSlack = 0.05f;

    // Extra seconds a projectile waits for its reports after it could have landed. The reports travel
    // with the same delay as the launch, so this only covers jitter and frozen frames (as PlayerAutoAttack).
    private const double ReportGrace = 1.0;

    [SerializeField] private SkillCastSettings _castSettings;
    [SerializeField] private ElementLibrary _elements;

    private PlayerElement _element;
    private PlayerCharacter _character;
    private PlayerDowned _downed;
    private PlayerStats _stats;
    private PlayerUpgradeEvents _events;
    private InputService _input;
    private ContactFilter2D _targetFilter;

    // Owner side, in Time.time. The cast times are for the floor of a cooldown given back (#161).
    private readonly float[] _readyTimes = new float[SlotCount];
    private readonly float[] _castTimes = new float[SlotCount];
    private readonly List<Collider2D> _candidates = new();
    private readonly List<Health> _targets = new();

    // Server side, in real time: casts arrive at the pace of the client's clock, not the host's frames.
    private readonly double[] _serverReadyTimes = new double[SlotCount];
    private readonly double[] _serverCastTimes = new double[SlotCount];
    private readonly HashSet<Health> _appliedTargets = new();
    private readonly List<Collider2D> _ringCandidates = new();
    private readonly Dictionary<ushort, SkillShotRecord> _pendingShots = new();
    private readonly List<ushort> _expiredShotIds = new();

    // Owner side: the id of the next projectile, so its reports name it.
    private ushort _nextCastId;

    private void Awake()
    {
        _element = GetComponent<PlayerElement>();
        _character = GetComponent<PlayerCharacter>();

        // RequireComponent only adds it to new objects: a prefab made before #162 needs it added by hand.
        if (_character == null)
            Debug.LogError("[PlayerSkills] The player has no PlayerCharacter: it has no skills.", this);

        // Optional: without it the player can never be downed (#51).
        TryGetComponent(out _downed);

        // Optional: without them no upgrade changes the skills or hears about them (#161).
        TryGetComponent(out _stats);
        TryGetComponent(out _events);

        if (_castSettings == null || _elements == null)
        {
            Debug.LogError("[PlayerSkills] Cast settings or element library are not assigned.", this);
            return;
        }

        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(_castSettings.TargetLayers);
        _targetFilter.useTriggers = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled players come back with the cooldowns of their previous life.
        Array.Clear(_serverReadyTimes, 0, _serverReadyTimes.Length);
        Array.Clear(_serverCastTimes, 0, _serverCastTimes.Length);
        _pendingShots.Clear();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Array.Clear(_readyTimes, 0, _readyTimes.Length);
        Array.Clear(_castTimes, 0, _castTimes.Length);

        if (IsOwner && GameServices.Instance != null)
            _input = GameServices.Instance.InputService;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _input = null;
    }

    // Polled here rather than through input callbacks: the check for the mouse
    // being over the UI isn't reliable inside a callback, and Unity warns about it.
    private void Update()
    {
        if (!IsOwner || _input == null)
            return;

        if (_input.Skill1.WasPressedThisFrame())
            TryCast(AttackSlot.Skill1);

        if (_input.Skill2.WasPressedThisFrame())
            TryCast(AttackSlot.Skill2);

        if (_input.Skill3.WasPressedThisFrame())
            TryCast(AttackSlot.Skill3);
    }

    /// <summary>
    /// Null for an empty slot.
    /// </summary>
    public SkillDefinition GetSkill(AttackSlot slot)
    {
        return GetSkillAt(ToIndex(slot));
    }

    private SkillDefinition GetSkillAt(int index)
    {
        CharacterDefinition character = _character != null ? _character.Definition : null;
        return character != null ? character.GetStartingSkill(index) : null;
    }

    /// <summary>
    /// Owner side. Seconds until that slot can be cast again, for the HUD (#73).
    /// </summary>
    public float GetCooldownRemaining(AttackSlot slot)
    {
        int index = ToIndex(slot);
        if (index < 0 || index >= SlotCount)
            return 0f;

        return Mathf.Max(0f, _readyTimes[index] - Time.time);
    }

    private void TryCast(AttackSlot slot)
    {
        SkillDefinition skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || _castSettings == null || Time.time < _readyTimes[index])
            return;

        // A downed player can't cast (#51).
        if (_downed != null && _downed.IsDowned)
            return;

        // A click on a menu button is not a cast.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 origin = transform.position;
        if (!TryGetAimPoint(skill, origin, out Vector2 point))
            return;

        _castTimes[index] = Time.time;
        _readyTimes[index] = Time.time + skill.Cooldown;
        LauncherEvents.AbilityCast(skill.Id, skill.GetElement(_element.Current));

        // The tick goes with the cast, so a timed effect it gives (Tempo) starts and ends on the same
        // tick on the host and here (#157).
        uint tick = TimeManager.Tick;

        if (skill.IsProjectile)
        {
            LaunchOwn(slot, skill, origin, point, tick);
        }
        else
        {
            SkillArea area = SkillArea.For(skill, origin, point);
            FindTargets(area);
            ShowCast(skill, area);

            // The host's own player runs the server's checks right here.
            if (IsServerInitialized)
                CastOnServer(slot, origin, point, _targets, tick);
            else
                ServerCast(slot, origin, point, _targets, tick);
        }

        if (IsServerInitialized)
            return;

        // Lo tuyo, al instante: the owner shows the effects of its own cast without waiting (#157).
        if (_events != null)
            _events.PredictSkillCast(tick);
    }

    private bool TryGetAimPoint(SkillDefinition skill, Vector2 origin, out Vector2 point)
    {
        point = default;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        // The camera is orthographic, so the depth of the screen point doesn't matter.
        Vector2 screenPosition = _input.Aim.ReadValue<Vector2>();
        Vector2 cursor = camera.ScreenToWorldPoint(screenPosition);
        Vector2 toCursor = cursor - origin;

        // A line or a cone only needs a direction: with the cursor right on the player there's none.
        if (skill.IsAimedByDirection)
        {
            if (toCursor.sqrMagnitude < MinAimDistance * MinAimDistance)
                return false;

            point = origin + toCursor.normalized * skill.Length;
            return true;
        }

        // Aiming beyond the range drops it at the range, in the same direction.
        point = origin + Vector2.ClampMagnitude(toCursor, skill.CastRange);
        return true;
    }

    // Every living enemy the owner sees touching the area. The host only checks this list.
    private void FindTargets(SkillArea area)
    {
        _targets.Clear();

        // A circle around the whole area first, then the exact shape.
        Physics2D.OverlapCircle(area.BoundsCenter, area.BoundsRadius, _targetFilter, _candidates);

        foreach (Collider2D candidate in _candidates)
        {
            Health health = candidate.GetComponentInParent<Health>();
            if (health == null || health.IsDead || _targets.Contains(health))
                continue;

            if (area.DistanceOutside(candidate) > 0f)
                continue;

            _targets.Add(health);

            if (_targets.Count >= _castSettings.MaxTargets)
                break;
        }
    }

    // A target that no longer exists arrives as null.
    [ServerRpc]
    private void ServerCast(AttackSlot slot, Vector2 origin, Vector2 point, List<Health> targets, uint tick)
    {
        CastOnServer(slot, origin, point, targets, tick);
    }

    private void CastOnServer(AttackSlot slot, Vector2 origin, Vector2 point, List<Health> targets, uint tick)
    {
        if (!TryAcceptCast(slot, origin, point, false, out SkillDefinition skill))
            return;

        SkillArea area = SkillArea.For(skill, origin, point);
        ApplyToTargets(skill, slot, area, targets);
        ApplyToRing(skill, slot, area);

        // The upgrades that listen to casts (Tempo, #161), from the tick the owner cast it on.
        if (_events != null)
            _events.RaiseSkillCast(ClampCastTick(tick));

        // The caster already drew it, so the host only draws a client's cast.
        if (!IsOwner)
            ShowCast(skill, area);

        ObserversCast(slot, origin, point);
    }

    // The checks every cast goes through on the host, and its cooldown. False, with a warning when it
    // isn't honest, if the cast doesn't count.
    private bool TryAcceptCast(AttackSlot slot, Vector2 origin, Vector2 point, bool projectile, out SkillDefinition skill)
    {
        skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || _castSettings == null || skill.IsProjectile != projectile)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected a cast from client {OwnerId}: slot {slot} has no skill of that kind.", this);
            return false;
        }

        // A cast sent just before the player went down still arrives. It's honest, so there's no warning,
        // but the host has the last word on who is downed (#51).
        if (_downed != null && _downed.IsDowned)
            return false;

        // The host sees this player slightly in the past, so where it says it stood gets some slack.
        float offset = Vector2.Distance(transform.position, origin);
        if (offset > _castSettings.PositionTolerance)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected {skill.Id} from client {OwnerId}: cast from {offset:0.00} units away from where the host sees it.", this);
            return false;
        }

        // The owner clamps a circle to its range and puts lines and cones at full length, so the aim point
        // has to agree with that, from the origin it sent (#162).
        float distance = Vector2.Distance(origin, point);
        bool aimFits = skill.IsAimedByDirection
            ? distance >= MinAimDistance
            : distance <= skill.CastRange + AimSlack;
        if (!aimFits)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected {skill.Id} from client {OwnerId}: aimed {distance:0.00} units away, which its shape doesn't allow.", this);
            return false;
        }

        double now = Time.realtimeSinceStartupAsDouble;
        if (now < _serverReadyTimes[index] - _castSettings.CooldownGrace)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected {skill.Id} from client {OwnerId}: cast before its cooldown ended.", this);
            return false;
        }

        _serverCastTimes[index] = now;
        _serverReadyTimes[index] = now + skill.Cooldown;
        return true;
    }

    private void ApplyToTargets(SkillDefinition skill, AttackSlot slot, SkillArea area, List<Health> targets)
    {
        _appliedTargets.Clear();

        if (targets == null)
            return;

        ElementType element = skill.GetElement(_element.Current);
        int applied = 0;

        foreach (Health target in targets)
        {
            if (applied >= _castSettings.MaxTargets)
            {
                Debug.LogWarning($"[PlayerSkills] Client {OwnerId} reported more than {_castSettings.MaxTargets} targets for {skill.Id}. The rest are ignored.", this);
                break;
            }

            // Gone, killed by someone else while the cast was on its way, or listed twice.
            if (target == null || target.IsDead || !_appliedTargets.Add(target))
                continue;

            if (!TryGetEnemyBody(target, out Collider2D body))
                continue;

            // Measured to the edge of the body, so its size barely matters. The enemy kept moving while the
            // cast was on its way, so it may be a little outside by now.
            float distance = area.DistanceOutside(body);
            if (distance > _castSettings.TargetTolerance)
            {
                Debug.LogWarning($"[PlayerSkills] Rejected {target.name} as a target of {skill.Id} from client {OwnerId}: {distance:0.00} units outside its {skill.Shape}.", this);
                continue;
            }

            applied++;
            HitTarget(skill, slot, area, target, element);
        }
    }

    // What a skill does to one enemy it hits, once the host has checked it: aura, damage and push.
    private void HitTarget(SkillDefinition skill, AttackSlot slot, SkillArea area, Health target, ElementType element)
    {
        // Read before the aura goes on: a detonation consumes it, and Cazador de auras still counts
        // (catalog engine rule 10).
        bool hasAura = target.TryGetComponent(out AuraHolder aura) && aura.HasAura;

        // The aura goes before the damage, so a killing blow still detonates.
        if (aura != null)
            aura.TryApply(element, skill.AuraStrength, NetworkObject, slot, skill.CanDetonate);

        // The reaction it just detonated may have killed it, and a dead enemy is already despawned.
        if (!target.IsDead)
            target.TryTakeDamage(GetDamage(skill, hasAura), NetworkObject, DamageKind.Skill, element);

        if (!target.IsDead)
            Push(skill, area, target);
    }

    // Ola (#162): straight away from where the caster stood. Only enemies with EnemyStatus can be moved,
    // so training dummies stay put, as with Sobrecarga.
    private static void Push(SkillDefinition skill, SkillArea area, Health target)
    {
        if (skill.Knockback <= 0f || !target.TryGetComponent(out EnemyStatus status))
            return;

        Vector2 away = (Vector2)target.transform.position - area.Origin;
        if (away.sqrMagnitude < 0.0001f)
            away = area.Direction;

        status.ApplyPush(away, skill.Knockback, skill.KnockbackDuration);
    }

    // Only enemies, so a wrong report can never hurt a teammate.
    private bool TryGetEnemyBody(Health target, out Collider2D body)
    {
        if (target.TryGetComponent(out body) && (_castSettings.TargetLayers.value & (1 << body.gameObject.layer)) != 0)
            return true;

        Debug.LogWarning($"[PlayerSkills] Rejected a target from client {OwnerId}: {target.name} has no collider on the target layers.", this);
        return false;
    }

    // The skill's damage with the player's upgrades (#161). Conditional bonuses need to know the target.
    private int GetDamage(SkillDefinition skill, bool targetHasAura)
    {
        if (_stats == null)
            return skill.Damage;

        TargetConditions met = targetHasAura ? TargetConditions.HasAura : TargetConditions.None;
        return Mathf.Max(0, Mathf.RoundToInt(_stats.Get(StatId.SkillDamage, skill.Damage, met)));
    }

    // Aura expansiva (#161): the skill's aura also lands on enemies just around its area, without damage
    // and never detonating (catalog engine rule 6), so it marks a crowd without setting off reactions.
    // Only the host looks for them, with what it sees: like the auras themselves, it's the host's decision.
    // The targets of the area are already in _appliedTargets, so nobody gets it twice. For a line or a
    // cone the ring is the same: everything up to that many units outside the shape (#162).
    private void ApplyToRing(SkillDefinition skill, AttackSlot slot, SkillArea area)
    {
        ElementType element = skill.GetElement(_element.Current);
        if (_stats == null || element == ElementType.None)
            return;

        float extraRadius = _stats.Get(StatId.SkillAuraExtraRadius, 0f);
        int extraTargets = Mathf.RoundToInt(_stats.Get(StatId.SkillAuraExtraTargets, 0f));
        if (extraRadius <= 0f || extraTargets <= 0)
            return;

        Physics2D.OverlapCircle(area.BoundsCenter, area.BoundsRadius + extraRadius, _targetFilter, _ringCandidates);

        int marked = 0;
        foreach (Collider2D candidate in _ringCandidates)
        {
            if (marked >= extraTargets)
                break;

            Health target = candidate.GetComponentInParent<Health>();
            if (target == null || target.IsDead || _appliedTargets.Contains(target))
                continue;

            if (area.DistanceOutside(candidate) > extraRadius || !_appliedTargets.Add(target))
                continue;

            if (target.TryGetComponent(out AuraHolder aura))
                aura.TryApply(element, skill.AuraStrength, NetworkObject, slot, canDetonate: false);

            marked++;
        }
    }

    // --- Projectiles (#162) ---

    // Owner side: our own projectile flies at once, and the host hears of the launch first. Its hits (Rayo)
    // or its burst (Llamarada, Burbuja) are reported as they happen, on the same reliable channel, so the
    // host always knows the launch before any report.
    private void LaunchOwn(AttackSlot slot, SkillDefinition skill, Vector2 origin, Vector2 point, uint tick)
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

        if (skill.Bursts)
        {
            projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius, 1,
                              _castSettings.TargetLayers, GetSkillColor(skill), 0f,
                              onStop: (stopPoint, hit) => ReportBurst(castId, skill, stopPoint, hit));
        }
        else
        {
            projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius,
                              1 + skill.Pierce, _castSettings.TargetLayers, GetSkillColor(skill), 0f,
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
    private void ReportBurst(ushort castId, SkillDefinition skill, Vector2 point, Collider2D hit)
    {
        if (this == null || !IsSpawned)
            return;

        // It reached the end of its range without touching anyone, and this skill only bursts on contact.
        if (hit == null && !skill.BurstAtEnd)
            return;

        SkillArea area = SkillArea.Circle(point, skill.ImpactRadius);
        FindTargets(area);
        ShowCast(skill, area);

        // The enemy it touched is the one Burbuja traps.
        Health touched = hit != null ? hit.GetComponentInParent<Health>() : null;

        if (IsServerInitialized)
            ApplyBurst(castId, point, _targets, touched);
        else
            ServerBurst(castId, point, _targets, touched);
    }

    [ServerRpc]
    private void ServerLaunch(AttackSlot slot, Vector2 origin, Vector2 point, ushort castId, uint tick)
    {
        LaunchOnServer(slot, origin, point, castId, tick);
    }

    private void LaunchOnServer(AttackSlot slot, Vector2 origin, Vector2 point, ushort castId, uint tick)
    {
        if (!TryAcceptCast(slot, origin, point, true, out SkillDefinition skill))
            return;

        ForgetExpiredShots();

        // Remembered until it could no longer land, plus the time its reports take to arrive.
        SkillArea path = SkillArea.For(skill, origin, point);
        double expiresAt = Time.realtimeSinceStartupAsDouble + skill.FlightTime + ReportGrace;
        _pendingShots[castId] = new SkillShotRecord(slot, skill, path, expiresAt, skill.Bursts ? 1 : 1 + skill.Pierce);

        if (_events != null)
            _events.RaiseSkillCast(ClampCastTick(tick));

        // The caster already flies its own; the host's copy of a client's projectile is only the picture.
        if (!IsOwner)
            LaunchCopy(skill, origin, point, (float)TimeManager.TimePassed(tick));

        ObserversLaunch(slot, origin, point, tick);
    }

    // Copies start late by the time the launch took to arrive, and catch up with the caster's.
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversLaunch(AttackSlot slot, Vector2 origin, Vector2 point, uint tick)
    {
        SkillDefinition skill = GetSkill(slot);
        if (skill != null && skill.IsProjectile)
            LaunchCopy(skill, origin, point, (float)TimeManager.TimePassed(tick));
    }

    private void LaunchCopy(SkillDefinition skill, Vector2 origin, Vector2 point, float timePassed)
    {
        SkillProjectile projectile = SpawnProjectile(skill, origin);
        if (projectile == null)
            return;

        int hits = skill.Bursts ? 1 : 1 + skill.Pierce;
        projectile.Launch(point - origin, skill.ProjectileSpeed, skill.Length, skill.ProjectileRadius, hits,
                          _castSettings.TargetLayers, GetSkillColor(skill), timePassed);
    }

    // A target that no longer exists arrives as null.
    [ServerRpc]
    private void ServerProjectileHit(ushort castId, Health target)
    {
        ApplyProjectileHit(castId, target);
    }

    private void ApplyProjectileHit(ushort castId, Health target)
    {
        // The projectile can outlive its caster.
        if (!IsServerInitialized || !TryGetShot(castId, false, out SkillShotRecord shot))
            return;

        // Someone else may have killed it while the report was on its way, and each enemy is hit once.
        if (target == null || target.IsDead || shot.Hit.Contains(target) || !TryGetEnemyBody(target, out Collider2D body))
            return;

        float distance = shot.Path.DistanceOutside(body);
        if (distance > _castSettings.TargetTolerance)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected a hit of {shot.Skill.Id} from client {OwnerId} on {target.name}: {distance:0.00} units from its path.", this);
            return;
        }

        shot.Hit.Add(target);
        shot.HitsLeft--;
        if (shot.HitsLeft <= 0)
            _pendingShots.Remove(castId);

        HitTarget(shot.Skill, shot.Slot, shot.Path, target, shot.Skill.GetElement(_element.Current));
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
        if (distance > _castSettings.TargetTolerance)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected the burst of {shot.Skill.Id} from client {OwnerId}: {distance:0.00} units from its path.", this);
            return;
        }

        SkillArea area = SkillArea.Circle(point, shot.Skill.ImpactRadius);
        ApplyToTargets(shot.Skill, shot.Slot, area, targets);
        Trap(shot.Skill, area, touched);
        ApplyToRing(shot.Skill, shot.Slot, area);

        // The caster already drew it, so the host only draws a client's burst.
        if (!IsOwner)
            ShowCast(shot.Skill, area);

        ObserversBurst(shot.Slot, point);
    }

    // Burbuja: the enemy it touched can't move or attack for a while. It's the stun of the reactions, so
    // EnemyStatus already stops its movement and its contact attack. Dummies have no EnemyStatus.
    private void Trap(SkillDefinition skill, SkillArea area, Health touched)
    {
        if (skill.TrapDuration <= 0f || touched == null || touched.IsDead || !TryGetEnemyBody(touched, out Collider2D body))
            return;

        if (area.DistanceOutside(body) > _castSettings.TargetTolerance)
            return;

        if (touched.TryGetComponent(out EnemyStatus status))
            status.ApplyStun(skill.TrapDuration);
    }

    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversBurst(AttackSlot slot, Vector2 point)
    {
        SkillDefinition skill = GetSkill(slot);
        if (skill != null && skill.Bursts)
            ShowCast(skill, SkillArea.Circle(point, skill.ImpactRadius));
    }

    // Unknown (never accepted, already used up or forgotten) or too old: the report doesn't count.
    private bool TryGetShot(ushort castId, bool bursts, out SkillShotRecord shot)
    {
        if (_pendingShots.TryGetValue(castId, out shot) && Time.realtimeSinceStartupAsDouble <= shot.ExpiresAt &&
            shot.Skill.Bursts == bursts)
            return true;

        Debug.LogWarning($"[PlayerSkills] Rejected a projectile report from client {OwnerId}: launch {castId} is unknown, used or too old.", this);
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

    private SkillProjectile SpawnProjectile(SkillDefinition skill, Vector2 origin)
    {
        if (skill.ProjectilePrefab == null)
        {
            Debug.LogError($"[PlayerSkills] {skill.Id} is a projectile without a Projectile Prefab.", skill);
            return null;
        }

        return Instantiate(skill.ProjectilePrefab, origin, Quaternion.identity);
    }

    // A client's tick is its estimate of the host's clock. It can't be in the future, and at most
    // MaxCastTickAge in the past: a made-up tick can't move a timed effect.
    private uint ClampCastTick(uint tick)
    {
        uint now = TimeManager.Tick;
        if (tick > now)
            return now;

        uint maxAge = TimeManager.TimeToTicks(_castSettings.MaxCastTickAge);
        return now - tick > maxAge ? now - maxAge : tick;
    }

    /// <summary>
    /// Server only. Takes seconds off every cooldown that is running, on the host and on the owner
    /// (#161, Relevo and Asistencia). Never below the floor: MinCooldownFraction of the full cooldown
    /// after the cast (catalog engine rule 1).
    /// </summary>
    public void RefundCooldowns(float seconds)
    {
        if (!IsServerInitialized || seconds <= 0f || _castSettings == null)
            return;

        float minFraction = _castSettings.MinCooldownFraction;
        double now = Time.realtimeSinceStartupAsDouble;
        for (int i = 0; i < SlotCount; i++)
        {
            // Only cooldowns that are running: a ready skill stays as it is.
            SkillDefinition skill = GetSkillAt(i);
            if (skill == null || _serverReadyTimes[i] <= now)
                continue;

            double floor = _serverCastTimes[i] + skill.Cooldown * minFraction;
            _serverReadyTimes[i] = Math.Max(floor, _serverReadyTimes[i] - seconds);
        }

        // The host's own player: its owner side is right here.
        if (IsOwner)
            RefundLocal(seconds);
        else
            TargetRefund(Owner, seconds);
    }

    // Each side works it out with its own clock and the same rule, so the HUD and the host agree.
    [TargetRpc]
    private void TargetRefund(NetworkConnection target, float seconds)
    {
        RefundLocal(seconds);
    }

    private void RefundLocal(float seconds)
    {
        float minFraction = _castSettings != null ? _castSettings.MinCooldownFraction : 0.6f;
        for (int i = 0; i < SlotCount; i++)
        {
            SkillDefinition skill = GetSkillAt(i);
            if (skill == null || _readyTimes[i] <= Time.time)
                continue;

            float floor = _castTimes[i] + skill.Cooldown * minFraction;
            _readyTimes[i] = Mathf.Max(floor, _readyTimes[i] - seconds);
        }
    }

    // The caster and the host already drew it.
    // With the origin the caster sent: a line or a cone starts where it stood when it cast, not where
    // this PC sees it now (#162).
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversCast(AttackSlot slot, Vector2 origin, Vector2 point)
    {
        SkillDefinition skill = GetSkill(slot);
        if (skill != null)
            ShowCast(skill, SkillArea.For(skill, origin, point));
    }

    private void ShowCast(SkillDefinition skill, SkillArea area)
    {
        if (skill.Visual == null)
            return;

        SkillShapeVisual visual = Instantiate(skill.Visual, area.Origin, Quaternion.identity);
        visual.Show(area, GetSkillColor(skill));
    }

    // The colour of the skill's element: a skill of another element shows that one, not the caster's.
    private Color GetSkillColor(SkillDefinition skill)
    {
        ElementDefinition element = _elements != null ? _elements.Get(skill.GetElement(_element.Current)) : null;
        return element != null ? element.Color : Color.white;
    }

    // Skill1 is slot 0. The basic attack has no slot here.
    private static int ToIndex(AttackSlot slot)
    {
        return (int)slot - (int)AttackSlot.Skill1;
    }
}
