using FishNet.Object;
using UnityEngine;

// What every enemy shares: its definition, its body and leaving the game when it dies.
// How it moves or attacks goes in separate components, like ChaserMovement.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : NetworkBehaviour
{
    [SerializeField] private EnemyDefinition _definition;

    private Health _health;
    private Rigidbody2D _body;

    public EnemyDefinition Definition => _definition;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _body = GetComponent<Rigidbody2D>();

        if (_definition == null)
            Debug.LogError("[Enemy] Enemy definition is not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _health.Died += OnDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        _health.Died -= OnDied;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Only the host simulates enemies. Elsewhere NetworkTransform moves them,
        // so their bodies just follow, like remote players.
        if (!IsServerInitialized)
        {
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    // Health sent the hit and the death to every client before raising Died,
    // so the enemy can leave right away.
    private void OnDied(Health health, NetworkObject attacker)
    {
        Despawn();
    }
}
