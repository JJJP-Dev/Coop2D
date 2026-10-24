using FishNet.Object;
using UnityEngine;

// Slows, stuns and pushes on an enemy, decided by the host. A simple version until the
// common crowd control rules. Movement components read it.
public class EnemyStatus : NetworkBehaviour
{
    // The catalog's cap: no slow takes more than 60 % of an enemy's speed.
    private const float MaxSlow = 0.6f;

    private float _slow;
    private float _slowUntil;
    private float _stunUntil;
    private Vector2 _pushVelocity;
    private float _pushUntil;

    /// <summary>
    /// Server side. 1 is full speed.
    /// </summary>
    public float SpeedMultiplier => Time.time < _slowUntil ? 1f - _slow : 1f;

    /// <summary>
    /// Server side.
    /// </summary>
    public bool IsStunned => Time.time < _stunUntil;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled and scene objects come back with the state of their previous life.
        _slowUntil = 0f;
        _stunUntil = 0f;
        _pushUntil = 0f;
    }

    /// <summary>
    /// Server side. While it returns true, a push moves the enemy:
    /// its movement should use this velocity instead of its own.
    /// </summary>
    public bool TryGetPushVelocity(out Vector2 velocity)
    {
        velocity = _pushVelocity;
        return Time.time < _pushUntil;
    }

    /// <summary>
    /// Server only. Slows don't add up: the strongest one applies. Amount goes from 0 to 1.
    /// </summary>
    public void ApplySlow(float amount, float duration)
    {
        if (!IsServerInitialized)
            return;

        float now = Time.time;
        if (now >= _slowUntil)
            _slow = 0f;

        _slow = Mathf.Max(_slow, Mathf.Clamp(amount, 0f, MaxSlow));
        _slowUntil = Mathf.Max(_slowUntil, now + duration);
    }

    /// <summary>
    /// Server only.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (IsServerInitialized)
            _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);
    }

    /// <summary>
    /// Server only. Moves the enemy that many units along the direction, over that many seconds.
    /// </summary>
    public void ApplyPush(Vector2 direction, float distance, float duration)
    {
        if (!IsServerInitialized || duration <= 0f)
            return;

        _pushVelocity = direction.normalized * (distance / duration);
        _pushUntil = Time.time + duration;
    }
}
