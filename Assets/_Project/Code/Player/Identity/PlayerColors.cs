using UnityEngine;

[CreateAssetMenu(fileName = "PlayerColors", menuName = "Coop2D/Player/Player Colors")]
public class PlayerColors : ScriptableObject
{
    // Placeholders until the shared palette is frozen (#44).
    [Tooltip("One color per player slot, from slot 0 to 3.")]
    [SerializeField]
    private Color[] _colors =
    {
        new Color(0.90f, 0.30f, 0.30f),
        new Color(0.30f, 0.55f, 0.95f),
        new Color(0.35f, 0.85f, 0.40f),
        new Color(0.95f, 0.85f, 0.30f),
    };

    public Color GetColor(int slot)
    {
        if (slot < 0 || slot >= _colors.Length)
            return Color.white;

        return _colors[slot];
    }

    private void OnValidate()
    {
        if (_colors.Length != PlayerSlots.MaxPlayers)
            Debug.LogWarning($"[PlayerColors] Expected {PlayerSlots.MaxPlayers} colors, found {_colors.Length}.", this);
    }
}
