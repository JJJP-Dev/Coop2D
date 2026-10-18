using UnityEngine;

[CreateAssetMenu(fileName = "PlayerElements", menuName = "Coop2D/Player/Player Elements")]
public class PlayerElements : ScriptableObject
{
    // Until characters exist (#162), each player slot plays a fixed element.
    [Tooltip("One element per player slot, from slot 0 to 3.")]
    [SerializeField]
    private ElementType[] _elements =
    {
        ElementType.Fire,
        ElementType.Water,
        ElementType.Electric,
        ElementType.Fire,
    };

    public ElementType GetElement(int slot)
    {
        if (slot < 0 || slot >= _elements.Length)
            return ElementType.None;

        return _elements[slot];
    }

    private void OnValidate()
    {
        if (_elements.Length != PlayerSlots.MaxPlayers)
            Debug.LogWarning($"[PlayerElements] Expected {PlayerSlots.MaxPlayers} elements, found {_elements.Length}.", this);
    }
}
