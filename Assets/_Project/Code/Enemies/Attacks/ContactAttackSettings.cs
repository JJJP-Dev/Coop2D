using UnityEngine;

// How hard and how often an enemy hurts the players it touches. One asset per kind of enemy
// that attacks this way; enemies that attack another way don't use it.
[CreateAssetMenu(fileName = "ContactAttackSettings", menuName = "Coop2D/Enemies/Contact Attack Settings")]
public class ContactAttackSettings : ScriptableObject
{
    [Tooltip("Health each touch takes. Players have 100.")]
    [SerializeField, Min(1)] private int _damage = 10;

    [Tooltip("How close the enemy's feet must be to a player's to touch them, in units (1 unit = 1 tile). " +
             "Keep it above the Stop Distance of its EnemyDefinition, or it stops before it can touch.")]
    [SerializeField, Min(0.1f)] private float _range = 0.75f;

    [Tooltip("Seconds this enemy waits after a touch before it can touch again. " +
             "The player's own invulnerability after a hit (0.5 s) also limits a crowd.")]
    [SerializeField, Min(0.1f)] private float _attackInterval = 1f;

    public int Damage => _damage;
    public float Range => _range;
    public float AttackInterval => _attackInterval;
}
