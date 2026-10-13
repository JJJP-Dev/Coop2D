using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlayerAutoAttack : NetworkBehaviour
{
    [SerializeField] private AutoAttackSettings _settings;

    private readonly List<Collider2D> _candidates = new();
    private ContactFilter2D _targetFilter;
    private float _nextAttackTime;

    private void Awake()
    {
        if (_settings == null || _settings.ProjectilePrefab == null)
        {
            Debug.LogError("[PlayerAutoAttack] Settings or their projectile prefab are not assigned.", this);
            return;
        }

        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(_settings.TargetLayers);
        _targetFilter.useTriggers = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (_settings != null)
            _nextAttackTime = Time.time + _settings.AttackInterval;
    }

    private void Update()
    {
        // Every copy of the player runs Update; only the server decides attacks.
        if (!IsServerInitialized || _settings == null || _settings.ProjectilePrefab == null)
            return;

        if (Time.time < _nextAttackTime)
            return;

        Vector2 origin = (Vector2)transform.position + _settings.FireOffset;

        // With nothing in range the attack stays ready and fires as soon as a target appears.
        if (!TryFindNearestTarget(origin, out Vector2 targetPosition))
            return;

        _nextAttackTime = Time.time + _settings.AttackInterval;
        Vector2 direction = (targetPosition - origin).normalized;

        SpawnProjectile(origin).LaunchOnServer(direction, _settings.Damage, NetworkObject);
        ObserversFire(origin, direction, TimeManager.Tick);
    }

    // The host already shows the server's copy, so its client is left out.
    [ObserversRpc(ExcludeServer = true)]
    private void ObserversFire(Vector2 origin, Vector2 direction, uint tick)
    {
        float timePassed = (float)TimeManager.TimePassed(tick);
        SpawnProjectile(origin).LaunchAsVisual(direction, timePassed);
    }

    private Projectile SpawnProjectile(Vector2 origin)
    {
        return Instantiate(_settings.ProjectilePrefab, origin, Quaternion.identity);
    }

    private bool TryFindNearestTarget(Vector2 origin, out Vector2 targetPosition)
    {
        targetPosition = default;
        float nearestSqrDistance = float.MaxValue;

        Physics2D.OverlapCircle(origin, _settings.Range, _targetFilter, _candidates);

        foreach (Collider2D candidate in _candidates)
        {
            Health health = candidate.GetComponentInParent<Health>();
            if (health == null || health.IsDead)
                continue;

            // Aim at the middle of the body, not at its feet.
            Vector2 position = candidate.bounds.center;
            float sqrDistance = (position - origin).sqrMagnitude;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                targetPosition = position;
            }
        }

        return nearestSqrDistance < float.MaxValue;
    }
}
