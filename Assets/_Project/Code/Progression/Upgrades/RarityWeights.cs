using System;
using UnityEngine;

// One row of the catalog's rarity table (#78, C.2): from this level on, how much each rarity weighs.
// They are weights, not percentages: they don't need to add up to 100.
[Serializable]
public class RarityWeights
{
    [Tooltip("Level of the level up from which this row applies, until the next row's.")]
    [SerializeField, Min(1)] private int _fromLevel = 2;

    [SerializeField, Min(0)] private int _common = 80;
    [SerializeField, Min(0)] private int _rare = 20;
    [SerializeField, Min(0)] private int _epic;

    [Tooltip("0 until the 0.2.")]
    [SerializeField, Min(0)] private int _legendary;

    // Unity creates rows with this one when a row is added in the Inspector.
    public RarityWeights()
    {
    }

    public RarityWeights(int fromLevel, int common, int rare, int epic, int legendary = 0)
    {
        _fromLevel = fromLevel;
        _common = common;
        _rare = rare;
        _epic = epic;
        _legendary = legendary;
    }

    public int FromLevel => _fromLevel;

    public int Get(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => _common,
            UpgradeRarity.Rare => _rare,
            UpgradeRarity.Epic => _epic,
            UpgradeRarity.Legendary => _legendary,
            _ => 0,
        };
    }
}
