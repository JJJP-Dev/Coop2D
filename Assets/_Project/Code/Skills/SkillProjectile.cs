using System;
using System.Collections.Generic;
using UnityEngine;

// What a projectile skill throws: Llamarada, Burbuja, Rayo. It works like the basic attack's
// Projectile: every PC flies its own copy, and only the caster's copy reports what it hits; the host
// checks each report against the path it remembers. It flies straight, so the host can check it.
// It hits each enemy once, stops after its last allowed hit, and also stops at the end of its range.
// It's drawn as big as it collides (Projectile Radius), so a bubble looks bigger than a bolt, and it can
// leave a trail of its colour.
[RequireComponent(typeof(SpriteRenderer))]
public class SkillProjectile : MonoBehaviour
{
    // A lag spike never makes a copy skip more than this.
    private const float MaxCatchUpTime = 0.3f;

    // Nearest first, so a projectile that stops on its first hit stops on the right enemy. Cached, so
    // sorting every frame doesn't allocate.
    private static readonly Comparison<RaycastHit2D> ByDistance = (a, b) => a.distance.CompareTo(b.distance);

    [Tooltip("Optional. A Trail Renderer on this object, tinted with the skill's colour. Skills whose data " +
             "says no trail switch it off.")]
    [SerializeField] private TrailRenderer _trail;

    [Tooltip("Width of the trail where it starts, as a share of the projectile's diameter.")]
    [SerializeField, Range(0.1f, 1f)] private float _trailWidth = 0.6f;

    private readonly List<RaycastHit2D> _hits = new();
    private readonly HashSet<Health> _alreadyHit = new();

    private ContactFilter2D _filter;
    private Vector2 _direction;
    private float _speed;
    private float _radius;
    private float _remaining;
    private float _catchUpTime;
    private int _hitsLeft;
    private Action<Collider2D> _onHit;
    private Action<Vector2, Collider2D> _onStop;
    private bool _launched;
    private bool _wantsTrail = true;

    /// <summary>
    /// Before Launch: whether this one leaves its trail.
    /// </summary>
    public void SetTrail(bool on)
    {
        _wantsTrail = on;
    }

    /// <summary>
    /// hits is how many enemies it hits before stopping (1 for one that bursts). onHit gets each enemy it
    /// hits; onStop, where it stopped and the enemy that stopped it, or null at the end of its range.
    /// Leave both null for a copy that is only the picture. timePassed is how late this copy starts:
    /// it moves at double speed until it catches up with the caster's.
    /// </summary>
    public void Launch(Vector2 direction, float speed, float range, float radius, int hits, LayerMask layers,
                       Color color, float timePassed, Action<Collider2D> onHit = null,
                       Action<Vector2, Collider2D> onStop = null)
    {
        _direction = direction.normalized;
        _speed = speed;
        _radius = radius;
        _remaining = range;
        _hitsLeft = Mathf.Max(1, hits);
        _catchUpTime = Mathf.Min(timePassed, MaxCatchUpTime);
        _onHit = onHit;
        _onStop = onStop;

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(layers);
        _filter.useTriggers = true;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.color = color;

        // The sprite's own size at scale 1, so any sprite ends up as wide as the projectile collides.
        float spriteSize = sprite.sprite != null ? Mathf.Max(0.01f, sprite.sprite.bounds.size.x) : 1f;
        transform.localScale = Vector3.one * (radius * 2f / spriteSize);

        if (_trail != null)
        {
            _trail.Clear();
            _trail.enabled = _wantsTrail;
            _trail.widthMultiplier = radius * 2f * _trailWidth;
            _trail.startColor = color;
            _trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        _alreadyHit.Clear();
        _launched = true;
    }

    private void Update()
    {
        if (!_launched)
            return;

        float step = Time.deltaTime;
        if (_catchUpTime > 0f)
        {
            float extra = Mathf.Min(_catchUpTime, Time.deltaTime);
            _catchUpTime -= extra;
            step += extra;
        }

        Vector2 position = transform.position;
        float distance = Mathf.Min(_speed * step, _remaining);

        // Casting along the whole step, so a fast projectile can't jump over an enemy.
        Physics2D.CircleCast(position, _radius, _direction, _filter, _hits, distance);
        _hits.Sort(ByDistance);

        foreach (RaycastHit2D hit in _hits)
        {
            Health health = hit.collider.GetComponentInParent<Health>();

            // Each enemy once, even with several colliders; the dead are already gone for the host.
            if (health == null || health.IsDead || !_alreadyHit.Add(health))
                continue;

            _onHit?.Invoke(hit.collider);
            _hitsLeft--;

            if (_hitsLeft <= 0)
            {
                Stop(hit.centroid, hit.collider);
                return;
            }
        }

        transform.position = position + _direction * distance;
        _remaining -= distance;

        if (_remaining <= 0f)
            Stop(transform.position, null);
    }

    private void Stop(Vector2 point, Collider2D hit)
    {
        _launched = false;
        _onStop?.Invoke(point, hit);
        Destroy(gameObject);
    }
}
