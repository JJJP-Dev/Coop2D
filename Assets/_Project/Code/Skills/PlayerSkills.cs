using System;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;

// The player's skills: aim with the mouse, cast with a button, wait for the cooldown.
[RequireComponent(typeof(PlayerElement))]
public class PlayerSkills : NetworkBehaviour
{
    public const int SlotCount = 3;

    [Tooltip("Slot 1 (left click), slot 2 (right click) and slot 3 (Q). " +
             "Until characters exist (#162), every player has the same ones.")]
    [SerializeField] private SkillDefinition[] _skills = new SkillDefinition[SlotCount];

    [SerializeField] private ElementLibrary _elements;

    private PlayerElement _element;
    private InputService _input;

    // Owner side, in Time.time.
    private readonly float[] _readyTimes = new float[SlotCount];

    private void Awake()
    {
        _element = GetComponent<PlayerElement>();

        if (_elements == null)
            Debug.LogError("[PlayerSkills] Element library is not assigned.", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Pooled players come back with the cooldowns of their previous life.
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

        if (skill == null || Time.time < _readyTimes[index])
            return;

        // A click on a menu button is not a cast.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!TryGetAimPoint(skill, out Vector2 point))
            return;

        _readyTimes[index] = Time.time + skill.Cooldown;
        ShowCast(skill, point);
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
