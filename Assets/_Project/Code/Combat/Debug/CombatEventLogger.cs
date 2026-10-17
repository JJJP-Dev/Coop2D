using FishNet.Object;
using UnityEngine;

// Test for the combat events: logs, numbered, every hit and death this client receives,
// so the counts can be compared between windows. Keep it disabled when not testing.
public class CombatEventLogger : MonoBehaviour
{
    private CombatEventService _events;
    private int _hitCount;
    private int _deathCount;

    private void OnEnable()
    {
        _events = GameServices.Instance.CombatEventService;
        _events.HitConfirmed += OnHitConfirmed;
        _events.DeathConfirmed += OnDeathConfirmed;
    }

    private void OnDisable()
    {
        if (_events == null)
            return;

        _events.HitConfirmed -= OnHitConfirmed;
        _events.DeathConfirmed -= OnDeathConfirmed;
        _events = null;
    }

    private void OnHitConfirmed(HitInfo hit)
    {
        _hitCount++;
        string mine = hit.ByLocalPlayer ? " (mine)" : "";
        Debug.Log($"[CombatEventLogger] Hit {_hitCount}: {Describe(hit.Target)} took {hit.Amount} from {Describe(hit.Attacker)} at {hit.Position}{mine}.", this);
    }

    private void OnDeathConfirmed(HitInfo hit)
    {
        _deathCount++;
        Debug.Log($"[CombatEventLogger] Death {_deathCount}: {Describe(hit.Target)}, killed by {Describe(hit.Attacker)}.", this);
    }

    private static string Describe(Health target)
    {
        return $"{target.name} (object {target.ObjectId})";
    }

    private static string Describe(NetworkObject attacker)
    {
        return attacker != null ? $"the player of client {attacker.OwnerId}" : "no one";
    }
}
