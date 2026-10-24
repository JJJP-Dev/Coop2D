using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;

// The player's skills. The owner aims and casts at once, with what it sees on its screen.
// The host checks the cast and applies its effects, and the other clients draw it.
// Which skills the player has is PlayerSkillSlots; this casts them.
// Where they hit: a circle, a line or a cone (SkillArea). The owner sends where it stood and where it
// aimed, and the host rebuilds the same area from those two points to check the targets.
// What a hit does is SkillHitResolver; skills that fly are PlayerSkillProjectiles; walls and turrets,
// which the host keeps making hit after the cast, are PlayerSkillPlacements.
[RequireComponent(typeof(PlayerElement))]
[RequireComponent(typeof(PlayerSkillSlots))]
public class PlayerSkills : NetworkBehaviour
{
    private const int SlotCount = PlayerSkillSlots.SlotCount;

    // A line or a cone needs a direction: closer than this, the cursor is on the player and gives none.
    private const float MinAimDistance = 0.1f;

    // A circle's aim point is clamped to its range on the owner; this only absorbs rounding.
    private const float AimSlack = 0.05f;

    [SerializeField] private SkillCastSettings _castSettings;
    [SerializeField] private ElementLibrary _elements;

    private PlayerElement _element;
    private PlayerSkillSlots _slots;
    private PlayerSkillProjectiles _projectiles;
    private PlayerSkillPlacements _placements;
    private PlayerDowned _downed;
    private PlayerStats _stats;
    private PlayerUpgradeEvents _events;
    private InputService _input;

    // Owner side, in Time.time. The cast times are for the floor of a cooldown given back.
    private readonly float[] _readyTimes = new float[SlotCount];
    private readonly float[] _castTimes = new float[SlotCount];

    // Server side, in real time: casts arrive at the pace of the client's clock, not the host's frames.
    private readonly double[] _serverReadyTimes = new double[SlotCount];
    private readonly double[] _serverCastTimes = new double[SlotCount];

    // What a wall or a turret sends instead of targets: the host finds them itself, hit after hit.
    private readonly List<Health> _noTargets = new();

    private void Awake()
    {
        _element = GetComponent<PlayerElement>();

        // RequireComponent only adds it to new objects: an older prefab needs it added by hand.
        if (!TryGetComponent(out _slots))
            Debug.LogError("[PlayerSkills] The player has no PlayerSkillSlots: it has no skills.", this);
        else
            _slots.SlotReplaced += OnSlotReplaced;

        if (!TryGetComponent(out _projectiles))
            Debug.LogError("[PlayerSkills] The player has no PlayerSkillProjectiles: its projectile skills do nothing.", this);

        if (!TryGetComponent(out _placements))
            Debug.LogError("[PlayerSkills] The player has no PlayerSkillPlacements: its walls and turrets do nothing.", this);

        // Optional: without it the player can never be downed.
        TryGetComponent(out _downed);

        // Optional: without them no upgrade changes the skills or hears about them.
        TryGetComponent(out _stats);
        TryGetComponent(out _events);

        if (_castSettings == null || _elements == null)
        {
            Debug.LogError("[PlayerSkills] Cast settings or element library are not assigned.", this);
            return;
        }

        Hits = new SkillHitResolver(this, _castSettings, _element, _stats);
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
    /// What this player's skills do to what they touch. Null if the cast settings are missing.
    /// </summary>
    public SkillHitResolver Hits { get; private set; }

    /// <summary>
    /// Every PC. Null for an empty slot.
    /// </summary>
    public SkillInstance GetSkill(AttackSlot slot)
    {
        return _slots != null ? _slots.Get(slot) : null;
    }

    private SkillInstance GetSkillAt(int index)
    {
        return _slots != null ? _slots.Get(index) : null;
    }

    // A slot that gets another skill starts ready: the cooldown of the one before isn't carried over.
    private void OnSlotReplaced(int index, bool asServer)
    {
        if (asServer)
        {
            _serverReadyTimes[index] = 0;
            _serverCastTimes[index] = 0;
        }
        else
        {
            _readyTimes[index] = 0f;
            _castTimes[index] = 0f;
        }
    }

    /// <summary>
    /// Owner side. Seconds until that slot can be cast again, for the HUD.
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
        SkillInstance skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || Hits == null || Time.time < _readyTimes[index])
            return;

        if ((skill.IsProjectile && _projectiles == null) || (skill.IsPlaced && _placements == null))
            return;

        // A downed player can't cast.
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
        // tick on the host and here.
        uint tick = TimeManager.Tick;

        if (skill.IsProjectile)
        {
            _projectiles.Launch(slot, skill, origin, point, tick);
        }
        else
        {
            SkillArea area = SkillArea.For(skill, origin, point);
            List<Health> targets = skill.IsPlaced ? _noTargets : Hits.FindTargets(area);
            Draw(slot, skill, area);

            // The host's own player runs the server's checks right here.
            if (IsServerInitialized)
                CastOnServer(slot, origin, point, targets, tick);
            else
                ServerCast(slot, origin, point, targets, tick);
        }

        if (IsServerInitialized)
            return;

        // The owner shows the effects of its own cast without waiting for the host.
        if (_events != null)
            _events.PredictSkillCast(tick);
    }

    private bool TryGetAimPoint(SkillInstance skill, Vector2 origin, out Vector2 point)
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

    // A target that no longer exists arrives as null.
    [ServerRpc]
    private void ServerCast(AttackSlot slot, Vector2 origin, Vector2 point, List<Health> targets, uint tick)
    {
        CastOnServer(slot, origin, point, targets, tick);
    }

    private void CastOnServer(AttackSlot slot, Vector2 origin, Vector2 point, List<Health> targets, uint tick)
    {
        if (!TryAcceptCast(slot, origin, point, false, out SkillInstance skill))
            return;

        SkillArea area = SkillArea.For(skill, origin, point);
        if (skill.IsPlaced)
            _placements.Place(slot, skill, area);
        else
            Hits.ApplyArea(skill, slot, area, targets);

        RaiseCastAccepted(tick);

        // The caster already drew it, so the host only draws a client's cast.
        if (!IsOwner)
            Draw(slot, skill, area);

        ObserversCast(slot, origin, point);
    }

    /// <summary>
    /// Server only. The checks every cast goes through on the host, and its cooldown. False, with a
    /// warning when it isn't honest, if the cast doesn't count.
    /// </summary>
    public bool TryAcceptCast(AttackSlot slot, Vector2 origin, Vector2 point, bool projectile, out SkillInstance skill)
    {
        skill = GetSkill(slot);
        int index = ToIndex(slot);

        if (skill == null || Hits == null || skill.IsProjectile != projectile || (skill.IsPlaced && _placements == null))
        {
            Debug.LogWarning($"[PlayerSkills] Rejected a cast from client {OwnerId}: slot {slot} has no skill of that kind.", this);
            return false;
        }

        // A cast sent just before the player went down still arrives. It's honest, so there's no warning,
        // but the host has the last word on who is downed.
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
        // has to agree with that, from the origin it sent.
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

    /// <summary>
    /// Server only, once a cast is accepted: the upgrades that listen to casts, from the tick the owner
    /// cast it on.
    /// </summary>
    public void RaiseCastAccepted(uint tick)
    {
        if (_events != null)
            _events.RaiseSkillCast(ClampCastTick(tick));
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
    /// (Relevo and Asistencia). Never below the floor: MinCooldownFraction of the full cooldown
    /// after the cast.
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
            SkillInstance skill = GetSkillAt(i);
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
            SkillInstance skill = GetSkillAt(i);
            if (skill == null || _readyTimes[i] <= Time.time)
                continue;

            float floor = _castTimes[i] + skill.Cooldown * minFraction;
            _readyTimes[i] = Mathf.Max(floor, _readyTimes[i] - seconds);
        }
    }

    // The caster and the host already drew it.
    // With the origin the caster sent: a line or a cone starts where it stood when it cast, not where
    // this PC sees it now.
    [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
    private void ObserversCast(AttackSlot slot, Vector2 origin, Vector2 point)
    {
        SkillInstance skill = GetSkill(slot);
        if (skill != null)
            Draw(slot, skill, SkillArea.For(skill, origin, point));
    }

    // A wall or a turret stays for its whole duration; everything else flashes and fades.
    private void Draw(AttackSlot slot, SkillInstance skill, SkillArea area)
    {
        if (skill.IsPlaced && _placements != null)
            _placements.Show(slot, skill, area);
        else
            ShowCast(skill, area);
    }

    /// <summary>
    /// Every PC. Draws the skill's shape where it landed: with an edge if it can detonate, and weaker on
    /// the PCs that aren't its caster's.
    /// </summary>
    public void ShowCast(SkillInstance skill, SkillArea area)
    {
        if (skill.Visual == null)
            return;

        SkillShapeVisual visual = Instantiate(skill.Visual, area.Origin, Quaternion.identity);
        visual.Show(area, GetSkillColor(skill), skill.CanDetonate, IsOwner);
    }

    /// <summary>
    /// The colour of the skill's element: a skill of another element shows that one, not the caster's.
    /// </summary>
    public Color GetSkillColor(SkillInstance skill)
    {
        ElementDefinition element = _elements != null ? _elements.Get(skill.GetElement(_element.Current)) : null;
        return element != null ? element.Color : Color.white;
    }

    private static int ToIndex(AttackSlot slot)
    {
        return PlayerSkillSlots.ToIndex(slot);
    }
}
