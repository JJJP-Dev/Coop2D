using System;
using System.Collections.Generic;
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
    private InputService _input;
    private ContactFilter2D _targetFilter;

    // Owner side, in Time.time.
    private readonly float[] _readyTimes = new float[SlotCount];
    private readonly List<Collider2D> _candidates = new();
    private readonly List<Health> _targets = new();

    // Server side, in real time: casts arrive at the pace of the client's clock, not the host's frames.
    private readonly double[] _serverReadyTimes = new double[SlotCount];
    private readonly HashSet<Health> _appliedTargets = new();

    private void Awake()
    {
        _element = GetComponent<PlayerElement>();

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
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Array.Clear(_readyTimes, 0, _readyTimes.Length);

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

        // A click on a menu button is not a cast.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!TryGetAimPoint(skill, out Vector2 point))
            return;

        _readyTimes[index] = Time.time + skill.Cooldown;
        LauncherEvents.AbilityCast(skill.Id, skill.GetElement(_element.Current));
        FindTargets(point, skill.Radius);
        ShowCast(skill, point);

        // The host's own player runs the server's checks right here.
        if (IsServerInitialized)
            CastOnServer(slot, point, _targets);
        else
            ServerCast(slot, point, _targets);
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
    private void ServerCast(AttackSlot slot, Vector2 point, List<Health> targets)
    {
        CastOnServer(slot, point, targets);
    }

    private void CastOnServer(AttackSlot slot, Vector2 point, List<Health> targets)
    {
        SkillDefinition skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || _castSettings == null)
        {
            Debug.LogWarning($"[PlayerSkills] Rejected a cast from client {OwnerId}: slot {slot} has no skill.", this);
            return;
        }

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

        _serverReadyTimes[index] = now + skill.Cooldown;
        ApplyToTargets(skill, slot, point, targets);

        // The caster already drew it, so the host only draws a client's cast.
        if (!IsOwner)
            ShowCast(skill, point);

        ObserversCast(slot, point);
    }

    private void ApplyToTargets(SkillDefinition skill, AttackSlot slot, Vector2 point, List<Health> targets)
    {
        if (targets == null)
            return;

        ElementType element = skill.GetElement(_element.Current);
        float reach = skill.Radius + _castSettings.TargetTolerance;
        int applied = 0;

        _appliedTargets.Clear();

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

            // The aura goes before the damage, so a killing blow still detonates.
            if (target.TryGetComponent(out AuraHolder aura))
                aura.TryApply(element, skill.AuraStrength, NetworkObject, slot, skill.CanDetonate);

            applied++;

            // The reaction it just detonated may have killed it, and a dead enemy is already despawned.
            if (!target.IsDead)
                target.TryTakeDamage(skill.Damage, NetworkObject, DamageKind.Skill, element);
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
