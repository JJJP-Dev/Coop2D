using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugCheatsSettings", menuName = "Coop2D/Cheats/Debug Cheats Settings")]
public class DebugCheatsSettings : ScriptableObject
{
    [Tooltip("Enemies the panel can spawn, one button each. They must be in FishNet's spawnable prefabs.")]
    [SerializeField] private List<Enemy> _enemies = new();

    [Tooltip("How far from the player who asks they appear, in units, in a random direction.")]
    [SerializeField, Min(0f)] private float _spawnDistance = 4f;

    public IReadOnlyList<Enemy> Enemies => _enemies;
    public float SpawnDistance => _spawnDistance;
}
