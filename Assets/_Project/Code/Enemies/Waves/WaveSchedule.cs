using System.Collections.Generic;
using UnityEngine;

// The enemies of a match over time. When its Duration is up nothing else comes, and once the last
// enemy dies the group has survived the match (MatchFlow).
[CreateAssetMenu(fileName = "WaveSchedule", menuName = "Coop2D/Enemies/Wave Schedule")]
public class WaveSchedule : ScriptableObject
{
    [Tooltip("Seconds. No enemy spawns after this, even if its wave hasn't finished.")]
    [SerializeField, Min(1f)] private float _duration = 180f;

    [Tooltip("While this many enemies are alive, the next one waits for a free place.")]
    [SerializeField, Min(1)] private int _maxAliveEnemies = 40;

    [Tooltip("Enemies spawn only at points at least this far from every player, in units. " +
             "If no point is that far, they use the farthest one.")]
    [SerializeField, Min(0f)] private float _minDistanceFromPlayers = 8f;

    [Tooltip("How many times each group's Count comes, by players: the first number for 1 player, the " +
             "second for 2... The same as the experience curve's, so more enemies give the experience " +
             "that more players need.")]
    [SerializeField] private float[] _countByPlayers = { 1f, 1.5f, 1.9f, 2.2f };

    [Tooltip("Times the enemies' health, by players, on top of each group's own Health Multiplier: the " +
             "first number for 1 player, the second for 2...")]
    [SerializeField] private float[] _healthByPlayers = { 1f, 1.2f, 1.35f, 1.5f };

    [Tooltip("They can overlap, and their order doesn't matter.")]
    [SerializeField] private List<WaveEntry> _waves = new();

    public float Duration => _duration;
    public int MaxAliveEnemies => _maxAliveEnemies;
    public float MinDistanceFromPlayers => _minDistanceFromPlayers;
    public IReadOnlyList<WaveEntry> Waves => _waves;

    /// <summary>
    /// How many enemies that group brings with this many players. Never less than one.
    /// </summary>
    public int GetCount(WaveEntry wave, int players)
    {
        return Mathf.Max(1, Mathf.RoundToInt(wave.Count * ByPlayers(_countByPlayers, players)));
    }

    /// <summary>
    /// The multiplier of that group's enemies' health with this many players.
    /// </summary>
    public float GetHealthMultiplier(WaveEntry wave, int players)
    {
        return wave.HealthMultiplier * ByPlayers(_healthByPlayers, players);
    }

    // The last value also covers more players than the list has.
    private static float ByPlayers(float[] values, int players)
    {
        return values != null && values.Length > 0 ? values[Mathf.Clamp(players, 1, values.Length) - 1] : 1f;
    }
}
