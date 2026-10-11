using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerIdentity : NetworkBehaviour
{
    private readonly SyncVar<int> _slot = new(-1);

    /// <summary>
    /// Player number from 0 to 3, assigned by the server. -1 until assigned.
    /// </summary>
    public int Slot => _slot.Value;

    // Called by the server before Spawn, so the value travels inside the spawn message.
    public void SetSlot(int slot)
    {
        _slot.Value = slot;
    }
}
