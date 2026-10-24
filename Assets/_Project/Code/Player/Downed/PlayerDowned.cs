using UnityEngine;

// Whether this player is downed. At 0 health a player doesn't die: it stays on the ground,
// can't move, shoot or cast, and waits for a teammate to get it up.
// Downed is simply its Health at 0. It travels with the health every client already receives, so
// there is no second value to keep in step, and whatever brings the health back (a revive, the
// Heal All cheat) gets the player up.
[RequireComponent(typeof(Health))]
public class PlayerDowned : MonoBehaviour
{
    private Health _health;

    /// <summary>
    /// True on every client, the host's included, once the host's 0 health has arrived there.
    /// </summary>
    public bool IsDowned => _health != null && _health.IsDead;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }
}
