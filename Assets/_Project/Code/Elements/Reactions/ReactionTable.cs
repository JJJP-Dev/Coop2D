using UnityEngine;

[CreateAssetMenu(fileName = "ReactionTable", menuName = "Coop2D/Elements/Reaction Table")]
public class ReactionTable : ScriptableObject
{
    [Tooltip("One row per ordered pair (aura, detonator). A pair without a row doesn't react.")]
    [SerializeField] private ReactionDefinition[] _reactions;

    [Tooltip("Seconds after a reaction in which the same enemy can't react again. " +
             "What reaches it meanwhile lands as if it couldn't detonate.")]
    [SerializeField, Min(0f)] private float _lockoutDuration = 0.5f;

    public float LockoutDuration => _lockoutDuration;

    public bool TryGet(ElementType auraElement, ElementType detonatorElement, out ReactionDefinition reaction)
    {
        foreach (ReactionDefinition row in _reactions)
        {
            if (row != null && row.AuraElement == auraElement && row.DetonatorElement == detonatorElement)
            {
                reaction = row;
                return true;
            }
        }

        reaction = null;
        return false;
    }

    private void OnValidate()
    {
        for (int i = 0; i < _reactions.Length; i++)
        {
            ReactionDefinition row = _reactions[i];
            if (row == null)
                continue;

            if (row.AuraElement == ElementType.None || row.AuraElement == row.DetonatorElement)
                Debug.LogWarning($"[ReactionTable] {row.name} needs two different elements.", this);

            for (int j = i + 1; j < _reactions.Length; j++)
            {
                ReactionDefinition other = _reactions[j];
                if (other != null && other.AuraElement == row.AuraElement && other.DetonatorElement == row.DetonatorElement)
                    Debug.LogWarning($"[ReactionTable] {row.name} and {other.name} have the same pair. Only the first is used.", this);
            }
        }
    }
}
