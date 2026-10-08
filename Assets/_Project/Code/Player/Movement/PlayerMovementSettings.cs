using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Coop2D/Player/Movement Settings")]
public class PlayerMovementSettings : ScriptableObject
{
    [Tooltip("Units per second (1 unit = 16 px).")]
    [SerializeField, Min(0f)] private float _maxSpeed = 5f;

    [Tooltip("Units per second squared, while there is input.")]
    [SerializeField, Min(0f)] private float _acceleration = 50f;

    [Tooltip("Units per second squared, with no input.")]
    [SerializeField, Min(0f)] private float _deceleration = 60f;

    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float Deceleration => _deceleration;
}
