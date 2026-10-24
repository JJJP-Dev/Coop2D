using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Coop2D/Enemies/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Tooltip("snake_case and unique: chaser... The launcher's events use it (enemy_killed, spawn_ cheats), " +
             "so once a build is out it never changes.")]
    [SerializeField] private string _id;

    [Tooltip("Units per second (1 unit = 16 px). Players move at 5.")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

    [Tooltip("Units per second squared. Lower values let a crowd push each other around more.")]
    [SerializeField, Min(0f)] private float _acceleration = 20f;

    [Tooltip("It stops getting closer when its feet are this far from its target's, in units.")]
    [SerializeField, Min(0f)] private float _stopDistance = 0.5f;

    [Tooltip("Experience its orb is worth when it dies. From the catalog: normal 1, veteran 3, elite 10, champion 20.")]
    [SerializeField, Min(0)] private int _experience = 1;

    public string Id => _id;
    public int Experience => _experience;
    public float MoveSpeed => _moveSpeed;
    public float Acceleration => _acceleration;
    public float StopDistance => _stopDistance;
}
