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

    [Tooltip("How far, in units, a shot may start from where the host sees the shooter. " +
             "The host sees other players slightly in the past, so 2 covers about 400 ms at full speed.")]
    [SerializeField, Min(0f)] private float _originTolerance = 2f;

    [Tooltip("How far, in units, a reported hit may be from the shot's path as the host sees it. " +
             "Enemies keep moving while the report travels to the host, so this covers that movement.")]
    [SerializeField, Min(0f)] private float _hitTolerance = 1.5f;

    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private Projectile _projectilePrefab;

    public float AttackInterval => _attackInterval;
    public float Range => _range;
    public int Damage => _damage;
    public Vector2 FireOffset => _fireOffset;
    public float OriginTolerance => _originTolerance;
    public float HitTolerance => _hitTolerance;
    public LayerMask TargetLayers => _targetLayers;
    public Projectile ProjectilePrefab => _projectilePrefab;
}
