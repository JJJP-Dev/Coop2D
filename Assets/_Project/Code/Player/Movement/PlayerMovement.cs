using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerMovementSettings _settings;

    private Rigidbody2D _body;
    private PlayerInputReader _input;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputReader>();

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

        _body.linearVelocity = CalculateVelocity(_body.linearVelocity, _input.MoveInput, Time.fixedDeltaTime);
    }

    // No networking here on purpose: client-side prediction will reuse it as is.
    private Vector2 CalculateVelocity(Vector2 currentVelocity, Vector2 moveInput, float deltaTime)
    {
        // Clamp, not normalize: a half-tilted stick should still walk at half speed.
        Vector2 direction = Vector2.ClampMagnitude(moveInput, 1f);
        Vector2 targetVelocity = direction * _settings.MaxSpeed;

        bool hasInput = direction.sqrMagnitude > 0.0001f;
        float rate = hasInput ? _settings.Acceleration : _settings.Deceleration;

        return Vector2.MoveTowards(currentVelocity, targetVelocity, rate * deltaTime);
    }
}
