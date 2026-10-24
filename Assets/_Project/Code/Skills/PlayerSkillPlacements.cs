using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

// The walls and turrets this player has on the ground. Only the host runs them: unlike a cast, nobody
// reports what they hit, because they keep hitting for seconds and the caster may be anywhere by then.
// The host checks every hit with what it sees, through the same SkillHitResolver as every other skill.
// Every PC draws the wall or the turret where it was cast, for as long as it lasts (Show, called by
// PlayerSkills with the cast it already sends). A turret's shots are the only thing that has to travel
// (ObserversShot), since only the host knows whom they hit.
[RequireComponent(typeof(PlayerSkills))]
public class PlayerSkillPlacements : NetworkBehaviour
{
    [Tooltip("Width of the line a turret's shot draws, in units.")]
    [SerializeField, Min(0.02f)] private float _shotWidth = 0.15f;

    [Tooltip("Radius of the circle that stands for a turret until it has art, in units.")]
    [SerializeField, Min(0.05f)] private float _postRadius = 0.35f;

    private PlayerSkills _skills;
    private PlayerSkillSlots _slots;

    // Server side.
    private readonly List<SkillPlacement> _placements = new();

    // Every PC: what it is drawing for each slot, to take it away early if the slot changes.
    private readonly List<SkillShapeVisual>[] _shown = { new(), new(), new() };

    private void Awake()
    {
        _skills = GetComponent<PlayerSkills>();

        if (TryGetComponent(out _slots))
            _slots.SlotReplaced += OnSlotReplaced;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled players come back with the walls of their previous life.
        _placements.Clear();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _placements.Clear();
    }

    /// <summary>
    /// Server only, once the host accepted the cast. From now on the host makes it hit.
    /// </summary>
    public void Place(AttackSlot slot, SkillInstance skill, SkillArea area)
    {
        if (!IsServerInitialized || skill == null || !skill.IsPlaced)
            return;

        _placements.Add(new SkillPlacement(slot, skill, area, Time.time));
    }

    /// <summary>
    /// Every PC. Draws a wall, or a turret with the reach it shoots in, for as long as it lasts, flashing
    /// on the rhythm of its hits.
    /// </summary>
    public void Show(AttackSlot slot, SkillInstance skill, SkillArea area)
    {
        int index = PlayerSkillSlots.ToIndex(slot);
        if (skill.Visual == null || index < 0 || index >= _shown.Length)
            return;

        Color color = _skills.GetSkillColor(skill);
        _shown[index].RemoveAll(visual => visual == null);

        if (skill.Shape == SkillShape.Turret)
        {
            // Its reach shows once, when it's placed, so everyone knows where it will shoot.
            SkillShapeVisual reach = Instantiate(skill.Visual, area.Point, Quaternion.identity);
            reach.Show(area, color, false, IsOwner);
            area = SkillArea.Circle(area.Point, _postRadius);
        }

        SkillShapeVisual lasting = Instantiate(skill.Visual, area.Origin, Quaternion.identity);
        lasting.ShowLasting(area, color, skill.Duration, skill.HitInterval, skill.CanDetonate, IsOwner);
        _shown[index].Add(lasting);
    }

    private void Update()
    {
        if (!IsServerInitialized || _placements.Count == 0 || _skills.Hits == null)
            return;

        float now = Time.time;

        // Backwards, so removing one doesn't skip the next.
        for (int i = _placements.Count - 1; i >= 0; i--)
        {
            SkillPlacement placement = _placements[i];
            if (now >= placement.EndsAt)
            {
                _placements.RemoveAt(i);
                continue;
            }

            // Never more than one hit per frame: a long frame doesn't make it hit twice at once.
            if (now < placement.NextHitAt)
                continue;

            placement.NextHitAt = Mathf.Max(placement.NextHitAt + placement.Skill.HitInterval, now);
            Hit(placement);
        }
    }

    private void Hit(SkillPlacement placement)
    {
        SkillInstance skill = placement.Skill;

        if (skill.Shape == SkillShape.Wall)
        {
            _skills.Hits.HitInside(skill, placement.Slot, placement.Area);
            return;
        }

        if (_skills.Hits.HitNearest(skill, placement.Slot, placement.Area.Point, placement.Area.Radius, out Vector2 hitPoint))
            ObserversShot(placement.Slot, placement.Area.Point, hitPoint);
    }

    // Every PC draws it, the host's own included.
    [ObserversRpc]
    private void ObserversShot(AttackSlot slot, Vector2 from, Vector2 to)
    {
        SkillInstance skill = _skills.GetSkill(slot);
        if (skill == null || skill.Visual == null)
            return;

        SkillArea line = SkillArea.Band(from, to, _shotWidth * 0.5f);
        SkillShapeVisual visual = Instantiate(skill.Visual, from, Quaternion.identity);
        visual.Show(line, _skills.GetSkillColor(skill), false, IsOwner);
    }

    // A slot that gets another skill, or every slot on a restart: what the old skill left goes with it.
    private void OnSlotReplaced(int index, bool asServer)
    {
        if (asServer)
        {
            AttackSlot slot = PlayerSkillSlots.ToSlot(index);
            _placements.RemoveAll(placement => placement.Slot == slot);
            return;
        }

        if (index < 0 || index >= _shown.Length)
            return;

        foreach (SkillShapeVisual visual in _shown[index])
        {
            if (visual != null)
                visual.Stop();
        }

        _shown[index].Clear();
    }
}
