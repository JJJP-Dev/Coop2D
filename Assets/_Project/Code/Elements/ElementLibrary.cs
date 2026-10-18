using UnityEngine;

[CreateAssetMenu(fileName = "ElementLibrary", menuName = "Coop2D/Elements/Element Library")]
public class ElementLibrary : ScriptableObject
{
    [Tooltip("One definition per element. Their order is the order of the test key.")]
    [SerializeField] private ElementDefinition[] _elements;

    /// <summary>
    /// Null for ElementType.None or an element without a definition.
    /// </summary>
    public ElementDefinition Get(ElementType type)
    {
        foreach (ElementDefinition element in _elements)
        {
            if (element != null && element.Type == type)
                return element;
        }

        return null;
    }

    /// <summary>
    /// The element after this one in the list, wrapping around.
    /// </summary>
    public ElementType GetNext(ElementType current)
    {
        int count = _elements.Length;
        int start = IndexOf(current);

        for (int step = 1; step <= count; step++)
        {
            ElementDefinition next = _elements[(start + step + count) % count];
            if (next != null)
                return next.Type;
        }

        return current;
    }

    private int IndexOf(ElementType type)
    {
        for (int i = 0; i < _elements.Length; i++)
        {
            if (_elements[i] != null && _elements[i].Type == type)
                return i;
        }

        // Not in the list: the next one is the first.
        return -1;
    }

    private void OnValidate()
    {
        for (int i = 0; i < _elements.Length; i++)
        {
            for (int j = i + 1; j < _elements.Length; j++)
            {
                if (_elements[i] != null && _elements[j] != null && _elements[i].Type == _elements[j].Type)
                    Debug.LogWarning($"[ElementLibrary] {_elements[i].Type} appears twice.", this);
            }
        }
    }
}
