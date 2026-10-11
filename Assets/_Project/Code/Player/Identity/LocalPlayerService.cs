using System;
using UnityEngine;

public class LocalPlayerService : MonoBehaviour
{
    /// <summary>
    /// The player this machine controls. Null while there is none.
    /// </summary>
    public PlayerIdentity Current { get; private set; }

    public event Action<PlayerIdentity> Changed;

    public void Register(PlayerIdentity player)
    {
        Current = player;
        Changed?.Invoke(Current);
    }

    public void Unregister(PlayerIdentity player)
    {
        if (Current != player)
            return;

        Current = null;
        Changed?.Invoke(null);
    }
}
