using System;
using UnityEngine;

// The catalog's cap of one stat: how far upgrades can take it. Multipliers are measured against
// the base value, so they keep working when a character has other bases. 0 means no limit.
[Serializable]
public class StatLimit
{
    [SerializeField] private StatId _stat;

    [Tooltip("Lowest the stat can go, as a multiple of its base. 0.6 is −40 %. 0: no limit.")]
    [SerializeField, Min(0f)] private float _minMultiplier;

    [Tooltip("Highest it can go, as a multiple of its base. 1.4 is +40 %. 0: no limit.")]
    [SerializeField, Min(0f)] private float _maxMultiplier;

    [Tooltip("Lowest value, whatever the base. 0: no limit.")]
    [SerializeField, Min(0f)] private float _min;

    [Tooltip("Highest value, whatever the base. 0: no limit.")]
    [SerializeField, Min(0f)] private float _max;

    // Unity creates rows with this one when a row is added in the Inspector.
    public StatLimit()
    {
    }

    public StatLimit(StatId stat, float minMultiplier, float maxMultiplier, float min = 0f, float max = 0f)
    {
        _stat = stat;
        _minMultiplier = minMultiplier;
        _maxMultiplier = maxMultiplier;
        _min = min;
        _max = max;
    }

    public StatId Stat => _stat;

    public float Clamp(float value, float baseValue)
    {
        if (_minMultiplier > 0f)
            value = Mathf.Max(value, baseValue * _minMultiplier);

        if (_maxMultiplier > 0f)
            value = Mathf.Min(value, baseValue * _maxMultiplier);

        if (_min > 0f)
            value = Mathf.Max(value, _min);

        if (_max > 0f)
            value = Mathf.Min(value, _max);

        return value;
    }
}
