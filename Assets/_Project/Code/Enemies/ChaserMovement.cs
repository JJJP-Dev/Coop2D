using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Walks straight at the nearest living player. Only the host runs it.
[RequireComponent(typeof(Enemy))]
public class ChaserMovement : NetworkBehaviour
{
    // Searching every physics step would be wasted work with dozens of enemies.
    private const float RetargetInterval = 0.25f;

    private Enemy _enemy;
    private Rigidbody2D _body;
    private EnemyStatus _status;
    private bool _wasPushed;
    private Transform _target;
    private float _nextRetargetTime;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _body = GetComponent<Rigidbody2D>();

        // Optional: without it, nothing can slow, stun or push this enemy.
        TryGetComponent(out _status);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _target = null;
        _nextRetargetTime = 0f;
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized)
            return;

        EnemyDefinition definition = _enemy.Definition;
        if (definition == null)
            return;

        // A push moves the body by itself.
        if (_status != null && _status.TryGetPushVelocity(out Vector2 pushVelocity))
        {
            _body.linearVelocity = pushVelocity;
            _wasPushed = true;
            return;
        }

        // It ends dead still, so it stays as short as its distance.
        if (_wasPushed)
        {
            _body.linearVelocity = Vector2.zero;
            _wasPushed = false;
        }

        if (_status != null && _status.IsStunned)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time >= _nextRetargetTime)
        {
            _target = FindNearestLivingPlayer();
            _nextRetargetTime = Time.time + RetargetInterval;
        }

        Vector2 targetVelocity = Vector2.zero;

        // A target that left the game reads as null here.
        if (_target != null)
        {
            Vector2 toTarget = (Vector2)_target.position - _body.position;
            float stopDistance = definition.StopDistance;

            if (toTarget.sqrMagnitude > stopDistance * stopDistance)
                targetVelocity = toTarget.normalized * (definition.MoveSpeed * GetSpeedMultiplier());
        }

        // Accelerating instead of setting the velocity leaves room for the pushes
        // between enemies, so a crowd spreads around its target.
        _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, targetVelocity, definition.Acceleration * Time.fixedDeltaTime);
    }

    private float GetSpeedMultiplier()
    {
        return _status != null ? _status.SpeedMultiplier : 1f;
    }

    private Transform FindNearestLivingPlayer()
    {
        Transform nearest = null;
        float nearestSqrDistance = float.MaxValue;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null)
                continue;

            if (player.TryGetComponent(out Health health) && health.IsDead)
                continue;

            float sqrDistance = ((Vector2)player.transform.position - _body.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = player.transform;
            }
        }

        return nearest;
    }
}
