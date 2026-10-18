using FishNet.Object;
using UnityEngine;

// Test for the combat events: logs, numbered, every hit, death and reaction this client receives,
// so the counts can be compared between windows. Keep it disabled when not testing.
public class CombatEventLogger : MonoBehaviour
{
    private CombatEventService _events;
    private int _hitCount;
    private int _deathCount;
    private int _reactionCount;

    private void OnEnable()
    {
        _events = GameServices.Instance.CombatEventService;
        _events.HitConfirmed += OnHitConfirmed;
        _events.DeathConfirmed += OnDeathConfirmed;
        _events.ReactionTriggered += OnReactionTriggered;
    }

    private void OnDisable()
    {
        if (_events == null)
            return;

        _events.HitConfirmed -= OnHitConfirmed;
        _events.DeathConfirmed -= OnDeathConfirmed;
        _events.ReactionTriggered -= OnReactionTriggered;
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

    private void OnReactionTriggered(ReactionInfo reaction)
    {
        _reactionCount++;
        string duo = reaction.IsDuo ? " Duo." : "";
        string mine = reaction.InvolvesLocalPlayer ? " (mine)" : "";
        Debug.Log($"[CombatEventLogger] Reaction {_reactionCount}: {reaction.Reaction.VariantId} on {Describe(reaction.Target)}, " +
                  $"aura by {Describe(reaction.AuraOwner)}, detonated by {Describe(reaction.Detonator)}.{duo}{mine}", this);
    }

    private static string Describe(NetworkBehaviour target)
    {
        return $"{target.name} (object {target.ObjectId})";
    }

    private static string Describe(NetworkObject player)
    {
        return player != null ? $"the player of client {player.OwnerId}" : "no one";
    }
}
