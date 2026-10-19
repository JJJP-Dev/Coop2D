using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugCheatsSettings", menuName = "Coop2D/Cheats/Debug Cheats Settings")]
public class DebugCheatsSettings : ScriptableObject
{
    [Tooltip("Enemies the panel can spawn, one button each. They must be in FishNet's spawnable prefabs.")]
    [SerializeField] private List<Enemy> _enemies = new();

    [Tooltip("How far from the player who asks they appear, in units, in a random direction.")]
    [SerializeField, Min(0f)] private float _spawnDistance = 4f;

    [Tooltip("One button per enemy and amount: 1 and 10 give «Generar Chaser» and «Generar 10 Chaser».")]
    [SerializeField] private List<int> _spawnAmounts = new() { 1, 10 };

    [Tooltip("Health that Damage All takes from everything that has it.")]
    [SerializeField, Min(1)] private int _damageAllAmount = 10;

    public IReadOnlyList<Enemy> Enemies => _enemies;
    public float SpawnDistance => _spawnDistance;
    public IReadOnlyList<int> SpawnAmounts => _spawnAmounts;
    public int DamageAllAmount => _damageAllAmount;
}
