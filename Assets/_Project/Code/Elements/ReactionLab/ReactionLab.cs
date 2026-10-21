using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// The reaction test scene (#147): put a weak or strong aura of any element on any dummy, detonate it
// with any element, and start over. Any player can use it, and the host carries it out with the
// real AuraHolder and ReactionService, so what comes out is what the game would do.
// The aura and the detonation count as the player who pressed the button, so two players can test duos.
// The basic attack starts switched off for everyone, so it doesn't mark the dummies while you watch.
public class ReactionLab : NetworkBehaviour
{
    [Tooltip("The dummies in this scene, in the order the panel lists them.")]
    [SerializeField] private AuraHolder[] _dummies;

    // The same on every client: each player's own client decides its shots (#156), so each one has to know.
    private readonly SyncVar<bool> _basicAttackEnabled = new(false);

    /// <summary>
    /// The lab of the match this client is in. Null outside the reaction test scene.
    /// </summary>
    public static ReactionLab Current { get; private set; }

    /// <summary>
    /// False only inside the lab, while its switch is off. PlayerAutoAttack asks it before each shot.
    /// </summary>
    public static bool BasicAttackAllowed => Current == null || Current._basicAttackEnabled.Value;

    public int DummyCount => _dummies != null ? _dummies.Length : 0;
    public bool BasicAttackEnabled => _basicAttackEnabled.Value;

    /// <summary>
    /// Raised on each client, host included, when the switch changes.
    /// </summary>
    public event Action<bool> BasicAttackChanged;

    /// <summary>
    /// Client side, on the client that pressed the button: what its last action did.
    /// </summary>
    public event Action<ReactionLabReport> Reported;

    private void Awake()
    {
        _basicAttackEnabled.OnChange += OnBasicAttackChanged;

        if (_dummies == null || _dummies.Length == 0)
            Debug.LogError("[ReactionLab] No dummies assigned.", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Current = this;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (Current == this)
            Current = null;
    }

    public string GetDummyName(int index)
    {
        return index >= 0 && index < DummyCount && _dummies[index] != null ? _dummies[index].name : "-";
    }

    // Called by the panel on its own client. Each one asks the host.
    public void ApplyAura(int dummyIndex, ElementType element, AuraStrength strength)
    {
        if (IsClientInitialized)
            ServerApplyAura(dummyIndex, element, strength);
    }

    public void Detonate(int dummyIndex, ElementType element)
    {
        if (IsClientInitialized)
            ServerDetonate(dummyIndex, element);
    }

    public void ResetDummies()
    {
        if (IsClientInitialized)
            ServerResetDummies();
    }

    public void ToggleBasicAttack()
    {
        if (IsClientInitialized)
            ServerToggleBasicAttack();
    }

    // Nobody owns this scene object, so it has to allow calls from anyone.
    // FishNet fills in caller with the connection that sent it.
    [ServerRpc(RequireOwnership = false)]
    private void ServerApplyAura(int dummyIndex, ElementType element, AuraStrength strength, NetworkConnection caller = null)
    {
        if (!TryGetDummy(dummyIndex, caller, out AuraHolder dummy))
            return;

        ReportLabUse(caller);

        // Weak like the basic attack, strong like a skill that only marks. Neither can detonate,
        // and each keeps its own aura cooldown, as in the game.
        AttackSlot attack = strength == AuraStrength.Weak ? AttackSlot.Basic : AttackSlot.Skill2;
        AuraApplyResult result = dummy.TryApply(element, strength, caller.FirstObject, attack, false);

        TargetReport(caller, new ReactionLabReport(ReactionLabAction.ApplyAura, dummyIndex, element, strength, result));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerDetonate(int dummyIndex, ElementType element, NetworkConnection caller = null)
    {
        if (!TryGetDummy(dummyIndex, caller, out AuraHolder dummy))
            return;

        ReportLabUse(caller);

        // A skill that detonates: strong, and the reaction's damage counts as this player's.
        AuraApplyResult result = dummy.TryApply(element, AuraStrength.Strong, caller.FirstObject, AttackSlot.Skill1, true);

        TargetReport(caller, new ReactionLabReport(ReactionLabAction.Detonate, dummyIndex, element, AuraStrength.Strong, result));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerResetDummies(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        ReportLabUse(caller);

        foreach (AuraHolder dummy in _dummies)
        {
            if (dummy == null || !dummy.IsSpawned)
                continue;

            dummy.GetComponent<Health>().RestoreFull();
            dummy.ResetForTesting();
        }

        TargetReport(caller, new ReactionLabReport(ReactionLabAction.Reset, -1, ElementType.None, AuraStrength.Weak, AuraApplyResult.Ignored));
    }

    // For everyone at once: one player's basic attack can mark a dummy another one is testing.
    [ServerRpc(RequireOwnership = false)]
    private void ServerToggleBasicAttack(NetworkConnection caller = null)
    {
        if (!IsAllowed(caller))
            return;

        _basicAttackEnabled.Value = !_basicAttackEnabled.Value;
        ReportLabUse(caller);
    }

    // For the launcher, the lab's buttons are a cheat: a reaction made with them is not one a player
    // played for. Every client writes it, so the match counts as one with cheats on every PC.
    private void ReportLabUse(NetworkConnection caller)
    {
        ObserversLabUsed(caller.ClientId);
    }

    [ObserversRpc]
    private void ObserversLabUsed(int callerClientId)
    {
        LauncherEvents.CheatUsed("reaction_lab", callerClientId == LocalConnection.ClientId);
    }

    private void OnBasicAttackChanged(bool previous, bool next, bool asServer)
    {
        if (!asServer)
            BasicAttackChanged?.Invoke(next);
    }

    // Only to whoever pressed it: the others see the reactions themselves, not the buttons.
    [TargetRpc]
    private void TargetReport(NetworkConnection target, ReactionLabReport report)
    {
        Reported?.Invoke(report);
    }

    private bool TryGetDummy(int index, NetworkConnection caller, out AuraHolder dummy)
    {
        dummy = null;

        if (!IsAllowed(caller))
            return false;

        if (index < 0 || index >= DummyCount || _dummies[index] == null || !_dummies[index].IsSpawned)
        {
            Debug.LogWarning($"[ReactionLab] Client {caller.ClientId} asked for dummy {index}, which isn't in the lab.", this);
            return false;
        }

        dummy = _dummies[index];
        return true;
    }

    // Like the cheats: a release build ignores it, even from a modified client.
    private bool IsAllowed(NetworkConnection caller)
    {
        if (Debug.isDebugBuild)
            return true;

        Debug.LogWarning($"[ReactionLab] Ignored a request from client {caller.ClientId}: this isn't a development build.", this);
        return false;
    }
}
