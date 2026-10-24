using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// What a player's skill does to the enemies it touches, kept apart from how the skill got there: an
// instant cast, a projectile's burst and, later, anything else that hits with a skill (a zone, a turret)
// all end here, so every hit follows the same rules.
// Plain C#: PlayerSkills builds one and lends it to whoever needs it.
// FindTargets runs on the caster's PC with what it sees; everything else runs on the host only, which
// checks those targets against its own view before applying aura, damage, push and trap.
public class SkillHitResolver
{
    private readonly NetworkBehaviour _caster;
    private readonly SkillCastSettings _settings;
    private readonly PlayerElement _element;
    private readonly PlayerStats _stats;
    private readonly ContactFilter2D _targetFilter;

    private readonly List<Collider2D> _candidates = new();
    private readonly List<Health> _targets = new();
    private readonly HashSet<Health> _appliedTargets = new();
    private readonly List<Collider2D> _ringCandidates = new();

    /// <param name="caster">The skill's owner: who the damage and the auras are credited to.</param>
    /// <param name="stats">Optional: without it no upgrade changes the damage or the aura ring.</param>
    public SkillHitResolver(NetworkBehaviour caster, SkillCastSettings settings, PlayerElement element, PlayerStats stats)
    {
        _caster = caster;
        _settings = settings;
        _element = element;
        _stats = stats;

        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(settings.TargetLayers);
        _targetFilter.useTriggers = true;
    }

    public LayerMask TargetLayers => _settings.TargetLayers;

    // How far outside its area the host still accepts a target, or a burst outside a projectile's path:
    // the enemy kept moving while the report was on its way.
    public float TargetTolerance => _settings.TargetTolerance;

    /// <summary>
    /// Caster's PC. Every living enemy it sees touching the area, up to MaxTargets. The list is reused by
    /// the next call, so it has to be sent or copied before that.
    /// </summary>
    public List<Health> FindTargets(SkillArea area)
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

            if (_targets.Count >= _settings.MaxTargets)
                break;
        }

        return _targets;
    }

    /// <summary>
    /// Host only. The targets the caster reported inside the area, each checked against where the host
    /// sees it; then the enemy it trapped, if any; then the aura ring around the area.
    /// </summary>
    /// <param name="trapped">The enemy a trapping skill touched; null for every other skill.</param>
    public void ApplyArea(SkillInstance skill, AttackSlot slot, SkillArea area, List<Health> targets, Health trapped = null)
    {
        ApplyToTargets(skill, slot, area, targets);
        Trap(skill, area, trapped);
        ApplyToRing(skill, slot, area);
    }

    /// <summary>
    /// Host only. One enemy a projectile reports hitting along its path. False if it doesn't count: not an
    /// enemy, already dead, or too far from the path for the host.
    /// </summary>
    public bool TryHitAlong(SkillInstance skill, AttackSlot slot, SkillArea path, Health target)
    {
        if (target == null || target.IsDead || !TryGetEnemyBody(target, out Collider2D body))
            return false;

        float distance = path.DistanceOutside(body);
        if (distance > _settings.TargetTolerance)
        {
            Debug.LogWarning($"[SkillHitResolver] Rejected a hit of {skill.Id} from client {_caster.OwnerId} on {target.name}: {distance:0.00} units from its path.", _caster);
            return false;
        }

        HitTarget(skill, slot, path, target, skill.GetElement(_element.Current));
        return true;
    }

    private void ApplyToTargets(SkillInstance skill, AttackSlot slot, SkillArea area, List<Health> targets)
    {
        _appliedTargets.Clear();

        if (targets == null)
            return;

        ElementType element = skill.GetElement(_element.Current);
        int applied = 0;

        foreach (Health target in targets)
        {
            if (applied >= _settings.MaxTargets)
            {
                Debug.LogWarning($"[SkillHitResolver] Client {_caster.OwnerId} reported more than {_settings.MaxTargets} targets for {skill.Id}. The rest are ignored.", _caster);
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
            if (distance > _settings.TargetTolerance)
            {
                Debug.LogWarning($"[SkillHitResolver] Rejected {target.name} as a target of {skill.Id} from client {_caster.OwnerId}: {distance:0.00} units outside its {skill.Shape}.", _caster);
                continue;
            }

            applied++;
            HitTarget(skill, slot, area, target, element);
        }
    }

    // Aura, damage and push on one enemy the host already checked.
    private void HitTarget(SkillInstance skill, AttackSlot slot, SkillArea area, Health target, ElementType element)
    {
        // Read before the aura goes on: a detonation consumes it, and a bonus against enemies with an aura
        // still counts.
        bool hasAura = target.TryGetComponent(out AuraHolder aura) && aura.HasAura;

        // The aura goes before the damage, so a killing blow still detonates.
        if (aura != null)
            aura.TryApply(element, skill.AuraStrength, _caster.NetworkObject, slot, skill.CanDetonate);

        // The reaction it just detonated may have killed it, and a dead enemy is already despawned.
        if (!target.IsDead)
            target.TryTakeDamage(GetDamage(skill, hasAura), _caster.NetworkObject, DamageKind.Skill, element);

        if (!target.IsDead)
            Push(skill, area, target);
    }

    // Straight away from where the caster stood. Only enemies with EnemyStatus can be moved, so training
    // dummies stay put.
    private static void Push(SkillInstance skill, SkillArea area, Health target)
    {
        if (skill.Knockback <= 0f || !target.TryGetComponent(out EnemyStatus status))
            return;

        Vector2 away = (Vector2)target.transform.position - area.Origin;
        if (away.sqrMagnitude < 0.0001f)
            away = area.Direction;

        status.ApplyPush(away, skill.Knockback, skill.KnockbackDuration);
    }

    // The enemy touched can't move or attack for a while. It's the stun of the reactions, so EnemyStatus
    // already stops its movement and its contact attack. Dummies have no EnemyStatus.
    private void Trap(SkillInstance skill, SkillArea area, Health touched)
    {
        if (skill.TrapDuration <= 0f || touched == null || touched.IsDead || !TryGetEnemyBody(touched, out Collider2D body))
            return;

        if (area.DistanceOutside(body) > _settings.TargetTolerance)
            return;

        if (touched.TryGetComponent(out EnemyStatus status))
            status.ApplyStun(skill.TrapDuration);
    }

    // The skill's aura also lands on enemies just around its area, without damage and never detonating,
    // so it marks a crowd without setting off reactions. Only the host looks for them, with what it sees.
    // The area's targets are already in _appliedTargets, so nobody gets it twice. For a line or a cone the
    // ring is the same: everything up to that many units outside the shape.
    private void ApplyToRing(SkillInstance skill, AttackSlot slot, SkillArea area)
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
                aura.TryApply(element, skill.AuraStrength, _caster.NetworkObject, slot, canDetonate: false);

            marked++;
        }
    }

    // The skill's damage with the player's upgrades. Conditional bonuses need to know the target.
    private int GetDamage(SkillInstance skill, bool targetHasAura)
    {
        if (_stats == null)
            return skill.Damage;

        TargetConditions met = targetHasAura ? TargetConditions.HasAura : TargetConditions.None;
        return Mathf.Max(0, Mathf.RoundToInt(_stats.Get(StatId.SkillDamage, skill.Damage, met)));
    }

    // Only enemies, so a wrong report can never hurt a teammate.
    private bool TryGetEnemyBody(Health target, out Collider2D body)
    {
        if (target.TryGetComponent(out body) && (_settings.TargetLayers.value & (1 << body.gameObject.layer)) != 0)
            return true;

        Debug.LogWarning($"[SkillHitResolver] Rejected a target from client {_caster.OwnerId}: {target.name} has no collider on the target layers.", _caster);
        return false;
    }
}
