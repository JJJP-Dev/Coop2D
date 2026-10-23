using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Which character this player plays (#162). The host sets it before the player is spawned, so it
// travels inside the spawn message and every PC knows it from the first frame, like the slot.
// Only the id travels; each PC finds the character's data in the same PlayerCharacters asset.
// The element (PlayerElement) and the skills (PlayerSkills) are read from it.
[RequireComponent(typeof(PlayerIdentity))]
public class PlayerCharacter : NetworkBehaviour
{
    [SerializeField] private PlayerCharacters _characters;

    private readonly SyncVar<string> _id = new();

    public string Id => _id.Value;

    /// <summary>
    /// Null until the host has set it, or if its id isn't in PlayerCharacters.
    /// </summary>
    public CharacterDefinition Definition => _characters != null ? _characters.Get(_id.Value) : null;

    private void Awake()
    {
        if (_characters == null)
            Debug.LogError("[PlayerCharacter] Player Characters is not assigned.", this);
    }

    /// <summary>
    /// Server only, before Spawn. The character of this player slot, until the lobby lets players
    /// choose (#173).
    /// </summary>
    public void SetFromSlot(int slot)
    {
        CharacterDefinition character = _characters != null ? _characters.GetForSlot(slot) : null;
        if (character == null)
        {
            Debug.LogError($"[PlayerCharacter] No character for slot {slot}.", this);
            return;
        }

        // Set before Spawn, the value goes inside the spawn message (networking.md 4.3). A pooled
        // player gets it again here, so it never keeps the one of its previous life.
        _id.Value = character.Id;
    }
}
