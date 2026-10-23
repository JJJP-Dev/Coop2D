using System;
using UnityEngine;

// The same prefab is used on every side. It only moves and finds what it hits;
// whoever launches it decides what that hit means.
public class Projectile : MonoBehaviour
{
    // A lag spike never makes a copy skip more than this.
    private const float MaxCatchUpTime = 0.3f;

    [SerializeField] private ProjectileSettings _settings;

    private Vector2 _direction;
    private Action<Collider2D> _onHit;
    private float _catchUpTime;
    private float _remainingLifetime;

    public ProjectileSettings Settings => _settings;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError("[Projectile] Projectile settings are not assigned.", this);
    }

    /// <summary>
    /// onHit receives the first collider it hits. Leave it null for a copy that is only the picture.
    /// reach multiplies how far it flies: the basic attack passes its range against the base one, so a
    /// longer range (#157) doesn't fire at enemies the shot can't reach.
    /// </summary>
    public void Launch(Vector2 direction, float timePassed, Action<Collider2D> onHit = null, float reach = 1f)
    {
        _direction = direction.normalized;
        _catchUpTime = Mathf.Min(timePassed, MaxCatchUpTime);
        _onHit = onHit;
        _remainingLifetime = _settings != null ? _settings.Lifetime * Mathf.Max(0.1f, reach) : 0f;
    }

    private void Update()
    {
        if (_settings == null)
        {
            Destroy(gameObject);
            return;
        }

        float step = Time.deltaTime;

        // A copy that starts late moves at double speed until it reaches
        // the point where the shooter's projectile is.
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
        _onHit?.Invoke(target);
        Destroy(gameObject);
    }
}
