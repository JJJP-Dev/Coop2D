using System;
using UnityEngine;

[Serializable]
public class WaveEntry
{
    [Tooltip("Seconds from the start of the match.")]
    [SerializeField, Min(0f)] private float _startTime;

    [SerializeField] private Enemy _enemyPrefab;

    [SerializeField, Min(1)] private int _count = 5;

    [Tooltip("Seconds between one enemy of the wave and the next. 0 spawns them all at once.")]
    [SerializeField, Min(0f)] private float _spawnInterval = 0.5f;

    public float StartTime => _startTime;
    public Enemy EnemyPrefab => _enemyPrefab;
    public int Count => _count;
    public float SpawnInterval => _spawnInterval;
}
