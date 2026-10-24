using UnityEngine;

// Every playable character, and the one each player slot plays for now.
// The lobby will let each player choose; until then the slot decides: J1 Brasa, J2 Marea, J3 Voltio,
// J4 Brasa.
[CreateAssetMenu(fileName = "PlayerCharacters", menuName = "Coop2D/Player/Player Characters")]
public class PlayerCharacters : ScriptableObject
{
    [Tooltip("Every character that can be played. A player's character travels over the network as its id, " +
             "and each PC finds it here: a character missing from this list can't be played.")]
    [SerializeField] private CharacterDefinition[] _characters;

    [Tooltip("The character of each player slot, from slot 0 (J1) to 3 (J4), until the lobby.")]
    [SerializeField] private CharacterDefinition[] _bySlot = new CharacterDefinition[PlayerSlots.MaxPlayers];

    public CharacterDefinition GetForSlot(int slot)
    {
        if (slot < 0 || slot >= _bySlot.Length)
            return null;

        return _bySlot[slot];
    }

    /// <summary>
    /// Null for an unknown id, or a null or empty one.
    /// </summary>
    public CharacterDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id) || _characters == null)
            return null;

        foreach (CharacterDefinition character in _characters)
        {
            if (character != null && character.Id == id)
                return character;
        }

        return null;
    }

    private void OnValidate()
    {
        if (_bySlot.Length != PlayerSlots.MaxPlayers)
            Debug.LogWarning($"[PlayerCharacters] Expected {PlayerSlots.MaxPlayers} slots, found {_bySlot.Length}.", this);

        // A slot's character has to be in the list, or the other PCs couldn't find it from its id.
        foreach (CharacterDefinition character in _bySlot)
        {
            if (character != null && Get(character.Id) != character)
                Debug.LogWarning($"[PlayerCharacters] {character.name} is given to a slot but isn't in Characters.", this);
        }
    }
}
