using UnityEngine;

// How fast this object is seen moving on this screen, measured frame by frame.
// Its Rigidbody2D can't tell: on clients NetworkTransform moves enemies through
// a kinematic body, whose velocity always reads zero.
public class MotionTracker : MonoBehaviour
{
    // Seconds the estimate takes to follow a change, so frame-to-frame jitter doesn't show.
    private const float SmoothingTime = 0.1f;

    // A faster jump between two frames is a teleport, not movement.
    private const float MaxTrackedSpeed = 30f;

    private Vector2 _lastPosition;
    private bool _hasLastPosition;

    public Vector2 Velocity { get; private set; }

    private void OnEnable()
    {
        _hasLastPosition = false;
        Velocity = Vector2.zero;
    }

    // After Update, when NetworkTransform or the physics interpolation has already moved it this frame.
    private void LateUpdate()
    {
        Vector2 position = transform.position;
        float deltaTime = Time.deltaTime;

        if (_hasLastPosition && deltaTime > 0f)
        {
            Vector2 frameVelocity = (position - _lastPosition) / deltaTime;

            if (frameVelocity.sqrMagnitude <= MaxTrackedSpeed * MaxTrackedSpeed)
            {
                float blend = 1f - Mathf.Exp(-deltaTime / SmoothingTime);
                Velocity = Vector2.Lerp(Velocity, frameVelocity, blend);
            }
        }

        _lastPosition = position;
        _hasLastPosition = true;
    }
}
