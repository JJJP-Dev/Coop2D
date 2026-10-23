using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;

// The player's skills. The owner aims and casts at once, with what it sees on its screen.
// The host checks the cast and applies its effects, and the other clients draw it.
[RequireComponent(typeof(PlayerElement))]
public class PlayerSkills : NetworkBehaviour
{
    public const int SlotCount = 3;

    [Tooltip("Slot 1 (left click), slot 2 (right click) and slot 3 (Q). " +
             "Until characters exist (#162), every player has the same ones.")]
    [SerializeField] private SkillDefinition[] _skills = new SkillDefinition[SlotCount];

    [SerializeField] private SkillCastSettings _castSettings;
    [SerializeField] private ElementLibrary _elements;

    private PlayerElement _element;
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

    private void Awake()
    {
        _element = GetComponent<PlayerElement>();

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
        int index = ToIndex(slot);
        return index >= 0 && index < _skills.Length ? _skills[index] : null;
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

        if (!TryGetAimPoint(skill, out Vector2 point))
            return;

        _castTimes[index] = Time.time;
        _readyTimes[index] = Time.time + skill.Cooldown;
        LauncherEvents.AbilityCast(skill.Id, skill.GetElement(_element.Current));
        FindTargets(point, skill.Radius);
        ShowCast(skill, point);

        // The tick goes with the cast, so a timed effect it gives (Tempo) starts and ends on the same
        // tick on the host and here (#157).
        uint tick = TimeManager.Tick;

        // The host's own player runs the server's checks right here.
        if (IsServerInitialized)
        {
            CastOnServer(slot, point, _targets, tick);
            return;
        }

        ServerCast(slot, point, _targets, tick);

        // Lo tuyo, al instante: the owner shows the effects of its own cast without waiting (#157).
        if (_events != null)
            _events.PredictSkillCast(tick);
    }

    private bool TryGetAimPoint(SkillDefinition skill, out Vector2 point)
    {
        point = default;

        Camera camera = Camera.main;
        if (camera == null)
            return false;

        // The camera is orthographic, so the depth of the screen point doesn't matter.
        Vector2 screenPosition = _input.Aim.ReadValue<Vector2>();
        Vector2 cursor = camera.ScreenToWorldPoint(screenPosition);
        Vector2 origin = transform.position;

        // Aiming beyond the range drops it at the range, in the same direction.
        point = origin + Vector2.ClampMagnitude(cursor - origin, skill.CastRange);
        return true;
    }

    // Every living enemy the owner sees inside the circle. The host only checks this list.
    private void FindTargets(Vector2 point, float radius)
    {
        _targets.Clear();
        Physics2D.OverlapCircle(point, radius, _targetFilter, _candidates);

        foreach (Collider2D candidate in _candidates)
        {
            Health health = candidate.GetComponentInParent<Health>();
            if (health == null || health.IsDead || _targets.Contains(health))
                continue;

            _targets.Add(health);

            if (_targets.Count >= _castSettings.MaxTargets)
                break;
        }
    }

    // A target that no longer exists arrives as null.
    [ServerRpc]
    private void ServerCast(AttackSlot slot, Vector2 point, List<Health> targets, uint tick)
    {
        CastOnServer(slot, point, targets, tick);
    }

    private void CastOnServer(AttackSlot slot, Vector2 point, List<Health> targets, uint tick)
    {
        SkillDefinition skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || _castSettings == null)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected a cast from client {OwnerId}: slot {slot} has no skill.", this);
            return;
        }

        // A cast sent just before the player went down still arrives. It's honest, so there's no warning,
        // but the host has the last word on who is downed (#51).
        if (_downed != null && _downed.IsDowned)
            return;

        // The host sees this player slightly in the past, so the range gets some slack.
        float distance = Vector2.Distance(transform.position, point);
        if (distance > skill.CastRange + _castSettings.PositionTolerance)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected {skill.Id} from client {OwnerId}: {distance:0.00} units away, beyond its range.", this);
            return;
        }

        double now = Time.realtimeSinceStartupAsDouble;
        if (now < _serverReadyTimes[index] - _castSettings.CooldownGrace)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected {skill.Id} from client {OwnerId}: cast before its cooldown ended.", this);
            return;
        }

        _serverCastTimes[index] = now;
        _serverReadyTimes[index] = now + skill.Cooldown;
        ApplyToTargets(skill, slot, point, targets);
        ApplyToRing(skill, slot, point);

        // The upgrades that listen to casts (Tempo, #161), from the tick the owner cast it on.
        if (_events != null)
            _events.RaiseSkillCast(ClampCastTick(tick));

        // The caster already drew it, so the host only draws a client's cast.
        if (!IsOwner)
            ShowCast(skill, point);

        ObserversCast(slot, point);
    }

    private void ApplyToTargets(SkillDefinition skill, AttackSlot slot, Vector2 point, List<Health> targets)
    {
        _appliedTargets.Clear();

        if (targets == null)
            return;

        ElementType element = skill.GetElement(_element.Current);
        float reach = skill.Radius + _castSettings.TargetTolerance;
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

            // Only enemies, so a wrong list can never hurt a teammate.
            if (!target.TryGetComponent(out Collider2D body) || (_castSettings.TargetLayers.value & (1 << body.gameObject.layer)) == 0)
            {
                Debug.LogWarning($"[PlayerSkills] Rejected a target from client {OwnerId}: {target.name} has no collider on the target layers.", this);
                continue;
            }

            // Measured to the edge of the body, so its size barely matters.
            float distance = Vector2.Distance(point, body.ClosestPoint(point));
            if (distance > reach)
            {
                Debug.LogWarning($"[PlayerSkills] Rejected {target.name} as a target of {skill.Id} from client {OwnerId}: {distance:0.00} units outside the circle.", this);
                continue;
            }

            // Read before the aura goes on: a detonation consumes it, and Cazador de auras still counts
            // (catalog engine rule 10).
            bool hasAura = target.TryGetComponent(out AuraHolder aura) && aura.HasAura;

            // The aura goes before the damage, so a killing blow still detonates.
            if (aura != null)
                aura.TryApply(element, skill.AuraStrength, NetworkObject, slot, skill.CanDetonate);

            applied++;

            // The reaction it just detonated may have killed it, and a dead enemy is already despawned.
            if (!target.IsDead)
                target.TryTakeDamage(GetDamage(skill, hasAura), NetworkObject, DamageKind.Skill, element);
        }
    }

    // The skill's damage with the player's upgrades (#161). Conditional bonuses need to know the target.
    private int GetDamage(SkillDefinition skill, bool targetHasAura)
    {
        if (_stats == null)
            return skill.Damage;

        TargetConditions met = targetHasAura ? TargetConditions.HasAura : TargetConditions.None;
        return Mathf.Max(0, Mathf.RoundToInt(_stats.Get(StatId.SkillDamage, skill.Damage, met)));
    }

    // Aura expansiva (#161): the skill's aura also lands on enemies just around its circle, without damage
    // and never detonating (catalog engine rule 6), so it marks a crowd without setting off reactions.
    // Only the host looks for them, with what it sees: like the auras themselves, it's the host's decision.
    // The targets of the circle are already in _appliedTargets, so nobody gets it twice.
    private void ApplyToRing(SkillDefinition skill, AttackSlot slot, Vector2 point)
    {
        ElementType element = skill.GetElement(_element.Current);
        if (_stats == null || element == ElementType.None)
            return;

        float extraRadius = _stats.Get(StatId.SkillAuraExtraRadius, 0f);
        int extraTargets = Mathf.RoundToInt(_stats.Get(StatId.SkillAuraExtraTargets, 0f));
        if (extraRadius <= 0f || extraTargets <= 0)
            return;

        float ringRadius = skill.Radius + extraRadius;
        Physics2D.OverlapCircle(point, ringRadius, _targetFilter, _ringCandidates);

        int marked = 0;
        foreach (Collider2D candidate in _ringCandidates)
        {
            if (marked >= extraTargets)
                break;

            Health target = candidate.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !_appliedTargets.Add(target))
                continue;

            if (target.TryGetComponent(out AuraHolder aura))
                aura.TryApply(element, skill.AuraStrength, NetworkObject, slot, canDetonate: false);

            marked++;
        }
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
            SkillDefinition skill = i < _skills.Length ? _skills[i] : null;
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
            SkillDefinition skill = i < _skills.Length ? _skills[i] : null;
            if (skill == null || _readyTimes[i] <= Time.time)
                continue;

            float floor = _castTimes[i] + skill.Cooldown * minFraction;
            _readyTimes[i] = Mathf.Max(floor, _readyTimes[i] - seconds);
        }
    }

    // The caster and the host already drew it.
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversCast(AttackSlot slot, Vector2 point)
    {
        SkillDefinition skill = GetSkill(slot);
        if (skill != null)
            ShowCast(skill, point);
    }

    private void ShowCast(SkillDefinition skill, Vector2 point)
    {
        if (skill.VisualPrefab == null)
            return;

        ElementDefinition element = _elements != null ? _elements.Get(skill.GetElement(_element.Current)) : null;
        Color color = element != null ? element.Color : Color.white;

        SkillCircleVisual visual = Instantiate(skill.VisualPrefab, point, Quaternion.identity);
        visual.Show(skill.Radius, color);
    }

    // Skill1 is slot 0. The basic attack has no slot here.
    private static int ToIndex(AttackSlot slot)
    {
        return (int)slot - (int)AttackSlot.Skill1;
    }

    private void OnValidate()
    {
        if (_skills.Length != SlotCount)
            Debug.LogWarning($"[PlayerSkills] Expected {SlotCount} skill slots, found {_skills.Length}.", this);
    }
}
