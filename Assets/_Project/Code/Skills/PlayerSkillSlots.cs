using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Which skills this player has: up to three, in the order they were got, each with its level.
// The match starts with the character's starting skill and the offers add the rest.
// The host keeps them in a list every PC receives (id and level), because every PC draws this player's
// casts; each PC builds the same SkillInstance from it, so the host's checks and the pictures use the
// same numbers. Only the host changes the list.
[RequireComponent(typeof(PlayerCharacter))]
public class PlayerSkillSlots : NetworkBehaviour
{
    public const int SlotCount = 3;

    [Tooltip("Every skill, to find the ones this player has from their ids.")]
    [SerializeField] private SkillLibrary _library;

    private readonly SyncList<OwnedSkill> _owned = new();

    // Each slot's skill as built from _owned, rebuilt when its id or level changes.
    private readonly SkillInstance[] _instances = new SkillInstance[SlotCount];

    private PlayerCharacter _character;

    public SkillLibrary Library => _library;

    /// <summary>
    /// Every PC. How many slots have a skill.
    /// </summary>
    public int Count => _owned.Count;

    /// <summary>
    /// Every PC. Whether a new skill still fits.
    /// </summary>
    public bool HasFreeSlot => _owned.Count < SlotCount;

    /// <summary>
    /// Every PC. A skill or its level changed, once per batch of changes.
    /// </summary>
    public event Action SkillsChanged;

    /// <summary>
    /// Every PC, the host as server included (asServer). The skill in that slot index changed to another
    /// one, or was emptied: whatever it had (a cooldown) belongs to the old one. A level up doesn't count.
    /// </summary>
    public event Action<int, bool> SlotReplaced;

    private void Awake()
    {
        _owned.OnChange += OnOwnedChanged;
        _character = GetComponent<PlayerCharacter>();

        if (_library == null)
            Debug.LogError("[PlayerSkillSlots] The skill library is not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // The character was set before Spawn, so its starting skill goes in the spawn message too.
        ResetSkills();
    }

    /// <summary>
    /// Every PC. Slot index 0 to 2 (left click, right click, Q). Null for an empty slot.
    /// </summary>
    public SkillInstance Get(int index)
    {
        if (index < 0 || index >= SlotCount || _library == null)
            return null;

        if (index >= _owned.Count)
            return _instances[index] = null;

        OwnedSkill owned = _owned[index];
        SkillInstance cached = _instances[index];
        if (cached != null && cached.Id == owned.Id && cached.Level == owned.Level)
            return cached;

        SkillDefinition skill = _library.Get(owned.Id);
        if (skill == null)
        {
            // Host and clients run the same build, so this is a bug worth seeing.
            Debug.LogError($"[PlayerSkillSlots] Client {OwnerId} has skill «{owned.Id}», which isn't in the skill library.", this);
            return null;
        }

        return _instances[index] = new SkillInstance(skill, owned.Level);
    }

    /// <summary>
    /// Every PC. Null for an empty slot or the basic attack.
    /// </summary>
    public SkillInstance Get(AttackSlot slot)
    {
        return Get(ToIndex(slot));
    }

    /// <summary>
    /// Every PC. The level this player has that skill at, or 0 if they don't have it.
    /// </summary>
    public int GetLevel(string id)
    {
        foreach (OwnedSkill owned in _owned)
        {
            if (owned.Id == id)
                return owned.Level;
        }

        return 0;
    }

    /// <summary>
    /// Server only. The new skill goes into the first free slot, at level 1. False if there's no room,
    /// the player already has it, or it isn't in the library (every PC must be able to find it).
    /// </summary>
    public bool TryAddSkill(SkillDefinition skill)
    {
        if (!IsServerInitialized || _library == null || skill == null || !HasFreeSlot || GetLevel(skill.Id) > 0 ||
            _library.Get(skill.Id) != skill)
            return false;

        _owned.Add(new OwnedSkill(skill.Id, 1));
        return true;
    }

    /// <summary>
    /// Server only. One more level of a skill the player has, up to its maximum.
    /// </summary>
    public bool TryLevelUp(string id)
    {
        if (!IsServerInitialized || _library == null)
            return false;

        for (int i = 0; i < _owned.Count; i++)
        {
            SkillDefinition skill = _library.Get(_owned[i].Id);
            if (_owned[i].Id != id || skill == null || _owned[i].Level >= skill.MaxLevel)
                continue;

            _owned[i] = new OwnedSkill(id, (byte)(_owned[i].Level + 1));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Server only. For the cheats panel: these skills, at level 1, in this order, replacing what the
    /// player has. Skills missing from the library are skipped.
    /// </summary>
    public void SetSkills(IReadOnlyList<SkillDefinition> skills)
    {
        if (!IsServerInitialized || _library == null || skills == null)
            return;

        _owned.Clear();
        foreach (SkillDefinition skill in skills)
        {
            if (skill != null && _owned.Count < SlotCount && _library.Get(skill.Id) == skill && GetLevel(skill.Id) == 0)
                _owned.Add(new OwnedSkill(skill.Id, 1));
        }
    }

    /// <summary>
    /// Server only. Back to the character's starting skill alone, for a new match.
    /// </summary>
    public void ResetSkills()
    {
        if (!IsServerInitialized)
            return;

        _owned.Clear();
        CharacterDefinition character = _character != null ? _character.Definition : null;
        if (character != null && character.StartingSkill != null)
            TryAddSkill(character.StartingSkill);
    }

    /// <summary>
    /// Slot index 0 to 2 for Skill1 to Skill3; -1 for the basic attack.
    /// </summary>
    public static int ToIndex(AttackSlot slot)
    {
        return (int)slot - (int)AttackSlot.Skill1;
    }

    /// <summary>
    /// The AttackSlot of a slot index. AttackSlot is a byte enum, so an int can't be added to it directly.
    /// </summary>
    public static AttackSlot ToSlot(int index)
    {
        return (AttackSlot)((int)AttackSlot.Skill1 + index);
    }

    private void OnOwnedChanged(SyncListOperation operation, int index, OwnedSkill previous, OwnedSkill next, bool asServer)
    {
        switch (operation)
        {
            case SyncListOperation.Complete:
                if (!asServer)
                    SkillsChanged?.Invoke();
                break;

            case SyncListOperation.Clear:
                for (int i = 0; i < SlotCount; i++)
                    SlotReplaced?.Invoke(i, asServer);
                break;

            default:
                if (index >= 0 && index < SlotCount && previous.Id != next.Id)
                    SlotReplaced?.Invoke(index, asServer);
                break;
        }
    }
}
