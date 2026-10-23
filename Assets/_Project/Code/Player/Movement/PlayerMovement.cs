using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerMovementSettings _settings;

    private Rigidbody2D _body;
    private PlayerInputReader _input;
    private PlayerDowned _downed;
    private NetworkTransform _networkTransform;

    // Optional: without it the player moves at its settings' speed (#157).
    private PlayerStats _stats;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputReader>();

        // Optional: without it the player can never be downed (#51).
        TryGetComponent(out _downed);

        TryGetComponent(out _networkTransform);
        TryGetComponent(out _stats);

        if (_settings == null)
            Debug.LogError("[PlayerMovement] Movement settings are not assigned.", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Remote copies are moved by NetworkTransform. Kinematic, but still
        // simulated, so the host can detect hits on them.
        _body.bodyType = IsOwner ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        _body.interpolation = IsOwner ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || _settings == null)
            return;

        // A downed player stays where it fell until someone gets it up (#51).
        if (_downed != null && _downed.IsDowned)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        _body.linearVelocity = CalculateVelocity(_body.linearVelocity, _input.MoveInput, Time.fixedDeltaTime);
    }

    /// <summary>
    /// On the owner and on the host: top speed with the player's upgrades (#157). The owner uses its own
    /// copy of the stats, so a speed upgrade is felt at once, without waiting for the host.
    /// </summary>
    public float MaxSpeed => _settings == null ? 0f
        : _stats != null ? _stats.Get(StatId.MoveSpeed, _settings.MaxSpeed)
        : _settings.MaxSpeed;

    /// <summary>
    /// Server only. Moves this player to a point, for a restarted match (#52). The owner moves its own
    /// player (its NetworkTransform is client authoritative), so the host asks the owner to do it.
    /// </summary>
    public void PlaceAt(Vector2 position)
    {
        if (!IsServerInitialized)
            return;

        // The host's own player: the host is its owner.
        if (IsOwner)
            MoveTo(position);
        else
            TargetPlaceAt(Owner, position);
    }

    [TargetRpc]
    private void TargetPlaceAt(NetworkConnection target, Vector2 position)
    {
        MoveTo(position);
    }

    // Owner side. Teleport makes the others jump there instead of sliding across the arena.
    private void MoveTo(Vector2 position)
    {
        if (_networkTransform != null)
            _networkTransform.Teleport();

        _body.position = position;
        _body.linearVelocity = Vector2.zero;
        transform.position = position;
    }

    // No networking here on purpose: client-side prediction will reuse it as is.
    private Vector2 CalculateVelocity(Vector2 currentVelocity, Vector2 moveInput, float deltaTime)
    {
        // Clamp, not normalize: a half-tilted stick should still walk at half speed.
        Vector2 direction = Vector2.ClampMagnitude(moveInput, 1f);
        Vector2 targetVelocity = direction * MaxSpeed;

        bool hasInput = direction.sqrMagnitude > 0.0001f;
        float rate = hasInput ? _settings.Acceleration : _settings.Deceleration;

        return Vector2.MoveTowards(currentVelocity, targetVelocity, rate * deltaTime);
    }
}
