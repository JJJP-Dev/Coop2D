using FishNet.Object;
using UnityEngine;

// The same prefab is used on every side. The server's copy deals the damage;
// the clients' copies are only the picture.
public class Projectile : MonoBehaviour
{
    // A lag spike never makes a client's copy skip more than this.
    private const float MaxCatchUpTime = 0.3f;

    [SerializeField] private ProjectileSettings _settings;

    private Vector2 _direction;
    private bool _dealsDamage;
    private int _damage;
    private NetworkObject _attacker;
    private float _catchUpTime;
    private float _remainingLifetime;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[Projectile] Projectile settings are not assigned.", this);
    }

    public void LaunchOnServer(Vector2 direction, int damage, NetworkObject attacker)
    {
        _dealsDamage = true;
        _damage = damage;
        _attacker = attacker;
        Launch(direction, 0f);
    }

    public void LaunchAsVisual(Vector2 direction, float timePassed)
    {
        _dealsDamage = false;
        Launch(direction, Mathf.Min(timePassed, MaxCatchUpTime));
    }

    private void Launch(Vector2 direction, float catchUpTime)
    {
        _direction = direction.normalized;
        _catchUpTime = catchUpTime;
        _remainingLifetime = _settings != null ? _settings.Lifetime : 0f;
    }

    private void Update()
    {
        if (_settings == null)
        {
            Destroy(gameObject);
            return;
        }

        float step = Time.deltaTime;

        // A client's copy starts late, so it moves at double speed until it
        // reaches the point where the server's copy is.
        if (_catchUpTime > 0f)
        {
            float extra = Mathf.Min(_catchUpTime, Time.deltaTime);
            _catchUpTime -= extra;
            step += extra;
        }

        Vector2 position = transform.position;
        float distance = _settings.Speed * step;

        // Casting along the whole step means a fast projectile can't jump over a target.
        RaycastHit2D hit = Physics2D.CircleCast(position, _settings.Radius, _direction, distance, _settings.HitLayers);
        if (hit.collider != null)
        {
            OnHit(hit.collider);
            return;
        }

        transform.position = position + _direction * distance;

        _remainingLifetime -= step;
        if (_remainingLifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnHit(Collider2D target)
    {
        if (_dealsDamage)
        {
            Health health = target.GetComponentInParent<Health>();
            if (health != null)
                health.TryTakeDamage(_damage, _attacker);
        }

        Destroy(gameObject);
    }
}
