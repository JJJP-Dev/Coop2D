using UnityEngine;

// How a player's stats are worked out (#157): the caps of the progression catalog (A.2). The base
// values stay in each system's own settings (movement, basic attack, health, orbs), so nothing is
// written twice.
[CreateAssetMenu(fileName = "PlayerStatSettings", menuName = "Coop2D/Player/Stat Settings")]
public class PlayerStatSettings : ScriptableObject
{
    [Tooltip("A stat without a row has no cap.")]
    [SerializeField]
    private StatLimit[] _limits =
    {
        new(StatId.MaxHealth, 0f, 0f, 50f, 250f),
        new(StatId.MoveSpeed, 0f, 1.4f),
        new(StatId.DamageTaken, 0.6f, 0f),
        new(StatId.PickupRadius, 0f, 2.5f),
        new(StatId.BasicInterval, 0.6f, 0f),
        new(StatId.BasicRange, 0f, 1.5f),
        new(StatId.BasicProjectiles, 0f, 0f, 0f, 4f),
    };

    /// <summary>
    /// The final value of a stat: (base + flat) × (1 + percent / 100), within its cap. The host and the
    /// owner call this same function with the same bonuses, so both get the same number.
    /// </summary>
    public float Calculate(StatId stat, float baseValue, float flat, float percent)
    {
        // Never below 0, even with more than −100 % in bonuses.
        float value = Mathf.Max(0f, (baseValue + flat) * Mathf.Max(0f, 1f + percent / 100f));

        foreach (StatLimit limit in _limits)
        {
            if (limit != null && limit.Stat == stat)
                return limit.Clamp(value, baseValue);
        }

        return value;
    }
}
