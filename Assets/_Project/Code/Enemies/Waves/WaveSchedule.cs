using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("They can overlap, and their order doesn't matter.")]
    [SerializeField] private List<WaveEntry> _waves = new();

    public float Duration => _duration;
    public int MaxAliveEnemies => _maxAliveEnemies;
    public float MinDistanceFromPlayers => _minDistanceFromPlayers;
    public IReadOnlyList<WaveEntry> Waves => _waves;
}
