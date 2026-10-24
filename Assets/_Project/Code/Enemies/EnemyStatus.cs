using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Slows, stuns and pushes on an enemy, decided by the host. A simple version until the
// common crowd control rules. Movement components read it.
// Only the stun travels, as the tick it ends on, so every PC can show which enemies can't move or
// attack (a Burbuja, a Sobrecarga) without a message each time one starts or ends.
public class EnemyStatus : NetworkBehaviour
{
    // The catalog's cap: no slow takes more than 60 % of an enemy's speed.
    private const float MaxSlow = 0.6f;

    private float _slow;
    private float _slowUntil;
    private float _stunUntil;
    private Vector2 _pushVelocity;
    private float _pushUntil;

    // In ticks of the host's clock, which every client follows: stunned while the tick is below it.
    private readonly SyncVar<uint> _stunEndTick = new();

    /// <summary>
    /// Server side. 1 is full speed.
    /// </summary>
    public float SpeedMultiplier => Time.time < _slowUntil ? 1f - _slow : 1f;

    /// <summary>
    /// Server side.
    /// </summary>
    public bool IsStunned => Time.time < _stunUntil;

    /// <summary>
    /// Every PC, for the picture. Can be a tick off the host's IsStunned.
    /// </summary>
    public bool LooksStunned => IsSpawned && TimeManager.Tick < _stunEndTick.Value;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Pooled and scene objects come back with the state of their previous life.
        _slowUntil = 0f;
        _stunUntil = 0f;
        _pushUntil = 0f;
        _stunEndTick.Value = 0;
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
        if (!IsServerInitialized)
            return;

        _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);

        uint endTick = TimeManager.Tick + TimeManager.TimeToTicks(duration);
        if (endTick > _stunEndTick.Value)
            _stunEndTick.Value = endTick;
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
