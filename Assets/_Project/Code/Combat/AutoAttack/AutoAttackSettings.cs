using UnityEngine;

[CreateAssetMenu(fileName = "AutoAttackSettings", menuName = "Coop2D/Combat/Auto Attack Settings")]
public class AutoAttackSettings : ScriptableObject
{
    [Tooltip("Seconds between shots.")]
    [SerializeField, Min(0.05f)] private float _attackInterval = 0.6f;

    [Tooltip("Only enemies closer than this are targeted, in units.")]
    [SerializeField, Min(0f)] private float _range = 6f;

    [SerializeField, Min(1)] private int _damage = 10;

    [Tooltip("Where shots start, measured from the character's feet.")]
    [SerializeField] private Vector2 _fireOffset = new(0f, 0.5f);

    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private Projectile _projectilePrefab;

    public float AttackInterval => _attackInterval;
    public float Range => _range;
    public int Damage => _damage;
    public Vector2 FireOffset => _fireOffset;
    public LayerMask TargetLayers => _targetLayers;
    public Projectile ProjectilePrefab => _projectilePrefab;
}
