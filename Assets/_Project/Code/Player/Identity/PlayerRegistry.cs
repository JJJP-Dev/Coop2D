using System;
using System.Collections.Generic;
using UnityEngine;

// Every player this client sees in the match, its own included. PlayerIdentity adds and removes itself.
// It only reflects what has arrived here: for the authority on who is connected, ask the host.
public class PlayerRegistry : MonoBehaviour
{
    private readonly List<PlayerIdentity> _players = new();

    public IReadOnlyList<PlayerIdentity> Players => _players;
    public int Count => _players.Count;

    public event Action<PlayerIdentity> Added;
    public event Action<PlayerIdentity> Removed;

    public void Add(PlayerIdentity player)
    {
        if (player == null || _players.Contains(player))
            return;

        _players.Add(player);
        Added?.Invoke(player);
    }

    public void Remove(PlayerIdentity player)
    {
        if (!_players.Remove(player))
            return;

        Removed?.Invoke(player);
    }
}
