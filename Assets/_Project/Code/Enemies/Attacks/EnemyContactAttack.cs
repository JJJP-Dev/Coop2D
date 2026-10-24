using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Hurts the living player this enemy touches. Only the host runs it: it decides every hit, and
// the player's Health sends the result to everyone. Enemies that attack another way don't have it,
// so each way of attacking is its own component with its own settings.
// Touching is a distance check rather than physics: on the host the remote players are kinematic,
// and with four players at most a few distances cost nothing.
[RequireComponent(typeof(Enemy))]
public class EnemyContactAttack : NetworkBehaviour
{
    // Checking every physics step would be wasted work with dozens of enemies.
    private const float CheckInterval = 0.1f;

    [SerializeField] private ContactAttackSettings _settings;

    private Health _health;
    private EnemyStatus _status;
    private float _nextCheckTime;
    private float _nextAttackTime;

    private void Awake()
    {
        _health = GetComponent<Health>();

        // Optional: without it, nothing can stun this enemy.
        TryGetComponent(out _status);

        if (_settings == null)
            Debug.LogError("[EnemyContactAttack] Contact attack settings are not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled enemies come back ready to attack, not with the wait of their previous life.
        _nextCheckTime = 0f;
        _nextAttackTime = 0f;
    }

    private void Update()
    {
        if (!IsServerInitialized || _settings == null)
            return;

        float now = Time.time;
        if (now < _nextCheckTime)
            return;

        _nextCheckTime = now + CheckInterval;

        // A stunned enemy can't attack either.
        if (now < _nextAttackTime || _health.IsDead || (_status != null && _status.IsStunned))
            return;

        Health target = FindTouchedPlayer();

        // A rejected hit (the player is still invulnerable from the last one) doesn't use up the attack,
        // so the enemy hits as soon as that ends.
        if (target != null && target.TryTakeDamage(_settings.Damage, NetworkObject, DamageKind.Contact))
            _nextAttackTime = now + _settings.AttackInterval;
    }

    // The nearest living player within range, or null. Downed players don't count.
    private Health FindTouchedPlayer()
    {
        Vector2 position = transform.position;
        float rangeSqr = _settings.Range * _settings.Range;
        Health nearest = null;
        float nearestSqrDistance = float.MaxValue;

        // Each connection's first object is its player (see PlayerSpawnService).
        foreach (NetworkConnection connection in ServerManager.Clients.Values)
        {
            NetworkObject player = connection.FirstObject;
            if (player == null || !player.TryGetComponent(out Health health) || health.IsDead)
                continue;

            float sqrDistance = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (sqrDistance <= rangeSqr && sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = health;
            }
        }

        return nearest;
    }
}
