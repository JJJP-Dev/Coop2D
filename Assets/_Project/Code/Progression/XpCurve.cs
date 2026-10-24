using UnityEngine;

// How much experience the group needs for each level, from the progression catalog:
// round((Base + PerLevel × (L − 1)) × M), with M the multiplier for the number of players. It is the
// same multiplier as the number of enemies: with 1.5 times more enemies, 1.5 times more
// experience keeps the same pace.
[CreateAssetMenu(fileName = "XpCurve", menuName = "Coop2D/Progression/Xp Curve")]
public class XpCurve : ScriptableObject
{
    [Tooltip("Experience to go from level 1 to 2 with one player.")]
    [SerializeField, Min(1)] private int _baseXp = 10;

    [Tooltip("How much more each level asks than the one before, with one player.")]
    [SerializeField, Min(0)] private int _xpPerLevel = 8;

    [Tooltip("Multiplier by players: the first number for 1 player, the second for 2... " +
             "More players than numbers use the last one.")]
    [SerializeField] private float[] _playerMultipliers = { 1f, 1.5f, 1.9f, 2.2f };

    /// <summary>
    /// Experience to go from this level to the next with this many players. At least 1.
    /// </summary>
    public int GetRequired(int level, int players)
    {
        int baseRequired = _baseXp + _xpPerLevel * (Mathf.Max(1, level) - 1);
        return Mathf.Max(1, Mathf.RoundToInt(baseRequired * GetMultiplier(players)));
    }

    private float GetMultiplier(int players)
    {
        if (_playerMultipliers == null || _playerMultipliers.Length == 0)
            return 1f;

        int index = Mathf.Clamp(players, 1, _playerMultipliers.Length) - 1;
        return Mathf.Max(0.01f, _playerMultipliers[index]);
    }
}
