using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSettings", menuName = "Coop2D/Combat/Projectile Settings")]
public class ProjectileSettings : ScriptableObject
{
    [Tooltip("Units per second.")]
    [SerializeField, Min(0.1f)] private float _speed = 10f;

    [Tooltip("Collision radius in units (0.125 = 2 px).")]
    [SerializeField, Min(0.01f)] private float _radius = 0.125f;

    [Tooltip("Seconds before it disappears if it hits nothing.")]
    [SerializeField, Min(0.05f)] private float _lifetime = 0.8f;

    [SerializeField] private LayerMask _hitLayers;

    public float Speed => _speed;
    public float Radius => _radius;
    public float Lifetime => _lifetime;
    public LayerMask HitLayers => _hitLayers;
}
