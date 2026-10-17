using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Coop2D/Enemies/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Tooltip("Units per second (1 unit = 16 px). Players move at 5.")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

    [Tooltip("Units per second squared. Lower values let a crowd push each other around more.")]
    [SerializeField, Min(0f)] private float _acceleration = 20f;

    [Tooltip("It stops getting closer when its feet are this far from its target's, in units.")]
    [SerializeField, Min(0f)] private float _stopDistance = 0.5f;

    public float MoveSpeed => _moveSpeed;
    public float Acceleration => _acceleration;
    public float StopDistance => _stopDistance;
}
