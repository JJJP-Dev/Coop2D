using System;
using FishNet.Connection;

public class PlayerSlots
{
    public const int MaxPlayers = 4;

    private readonly NetworkConnection[] _slotOwners = new NetworkConnection[MaxPlayers];

    public bool TryAssign(NetworkConnection conn, out int slot)
    {
        for (slot = 0; slot < MaxPlayers; slot++)
        {
            if (_slotOwners[slot] == null)
            {
                _slotOwners[slot] = conn;
                return true;
            }
        }

        slot = -1;
        return false;
    }

    public void Release(NetworkConnection conn)
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (_slotOwners[i] == conn)
                _slotOwners[i] = null;
        }
    }

    public void Clear()
    {
        Array.Clear(_slotOwners, 0, _slotOwners.Length);
    }
}
