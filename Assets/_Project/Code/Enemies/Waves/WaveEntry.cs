using System;
using UnityEngine;

// One group of one kind of enemy in a WaveSchedule. Groups that start at the same second are one wave
// for the players and the launcher: a wave can mix chasers and drones.
[Serializable]
public class WaveEntry
{
    [Tooltip("Seconds from the start of the match.")]
    [SerializeField, Min(0f)] private float _startTime;

    [SerializeField] private Enemy _enemyPrefab;

    [Tooltip("With one player. More players multiply it (Count By Players of the schedule).")]
    [SerializeField, Min(1)] private int _count = 5;

    [Tooltip("Seconds between one enemy of the wave and the next. 0 spawns them all at once.")]
    [SerializeField, Min(0f)] private float _spawnInterval = 0.5f;

    [Tooltip("Times the enemy's normal health: 1.5 makes a chaser of 60 come with 90. For late waves.")]
    [SerializeField, Min(0.1f)] private float _healthMultiplier = 1f;

    public float StartTime => _startTime;
    public Enemy EnemyPrefab => _enemyPrefab;
    public int Count => _count;
    public float SpawnInterval => _spawnInterval;
    public float HealthMultiplier => _healthMultiplier;
}
