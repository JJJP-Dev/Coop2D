using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerIdentity : NetworkBehaviour
{
    private readonly SyncVar<int> _slot = new(-1);

    private LocalPlayerService _localPlayerService;

    /// <summary>
    /// Player number from 0 to 3, assigned by the server. -1 until assigned.
    /// </summary>
    public int Slot => _slot.Value;

    // Called by the server before Spawn, so the value travels inside the spawn message.
    public void SetSlot(int slot)
    {
        _slot.Value = slot;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner || GameServices.Instance == null)
            return;

        _localPlayerService = GameServices.Instance.LocalPlayerService;
        _localPlayerService.Register(this);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (_localPlayerService != null)
            _localPlayerService.Unregister(this);

        _localPlayerService = null;
    }
}
