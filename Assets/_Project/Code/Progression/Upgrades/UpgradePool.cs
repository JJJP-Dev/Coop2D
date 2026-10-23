using System.Collections.Generic;
using UnityEngine;

// Every upgrade the offers can draw from (#78), and Respiro, which is not in the pool: it only comes
// when nothing else is left (catalog C.1). The host draws from it and the clients look up the ids
// they receive, so host and clients must have the same assets, which the same build guarantees.
[CreateAssetMenu(fileName = "UpgradePool", menuName = "Coop2D/Progression/Upgrade Pool")]
public class UpgradePool : ScriptableObject
{
    [Tooltip("Their order doesn't matter for the odds, but it is the order the offers go through them: " +
             "changing it changes what a seed offers.")]
    [SerializeField] private List<UpgradeDefinition> _upgrades = new();

    [Tooltip("Respiro: what a place of the offer gets when nothing else fits. Not in the list above.")]
    [SerializeField] private UpgradeDefinition _fallback;

    private Dictionary<string, UpgradeDefinition> _byId;

    public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;
    public UpgradeDefinition Fallback => _fallback;

    /// <summary>
    /// The upgrade with this id, Respiro included. False for an id this build doesn't have.
    /// </summary>
    public bool TryGet(string id, out UpgradeDefinition upgrade)
    {
        if (_byId == null)
            BuildIndex();

        upgrade = null;
        return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out upgrade);
    }

    private void OnEnable()
    {
        _byId = null;
    }

    private void BuildIndex()
    {
        _byId = new Dictionary<string, UpgradeDefinition>();

        foreach (UpgradeDefinition upgrade in _upgrades)
            Add(upgrade);

        Add(_fallback);
    }

    private void Add(UpgradeDefinition upgrade)
    {
        if (upgrade != null && !string.IsNullOrEmpty(upgrade.Id))
            _byId.TryAdd(upgrade.Id, upgrade);
    }

    private void OnValidate()
    {
        _byId = null;

        HashSet<string> ids = new();
        foreach (UpgradeDefinition upgrade in _upgrades)
        {
            if (upgrade == null)
                continue;

            if (string.IsNullOrEmpty(upgrade.Id))
                Debug.LogWarning($"[UpgradePool] {upgrade.name} has no id. It can't be offered.", this);
            else if (!ids.Add(upgrade.Id))
                Debug.LogWarning($"[UpgradePool] The id {upgrade.Id} is in the pool twice.", this);

            if (upgrade == _fallback)
                Debug.LogWarning($"[UpgradePool] {upgrade.name} is the fallback: take it out of the list.", this);
        }
    }
}
