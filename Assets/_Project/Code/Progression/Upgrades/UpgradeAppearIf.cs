using System;
using UnityEngine;

// The catalog's appearIf of an upgrade (#78): when in the match it can be offered at all.
// Only the conditions the Pre 3 upgrades use; the duo ones (pair of elements, reactions of the pair)
// come with the duo upgrades (#166).
[Serializable]
public class UpgradeAppearIf
{
    [Tooltip("First level of a level up that can offer it. 0: from the start.")]
    [SerializeField, Min(0)] private int _minLevel;

    [Tooltip("Last level that can offer it. 0: no limit. The economy ones stop at 8.")]
    [SerializeField, Min(0)] private int _maxLevel;

    [Tooltip("Players needed in the match. 0 or 1: also solo. The cooperation ones ask for 2.")]
    [SerializeField, Min(0)] private int _minPlayers;

    public int MinLevel => _minLevel;
    public int MaxLevel => _maxLevel;
    public int MinPlayers => _minPlayers;

    public bool Allows(int level, int players)
    {
        if (_minLevel > 0 && level < _minLevel)
            return false;

        if (_maxLevel > 0 && level > _maxLevel)
            return false;

        return players >= _minPlayers;
    }
}
