using System.Collections.Generic;
using UnityEngine;

public class DecorCatalog : MonoBehaviour
{
    public List<DecorItemDefinition> items = new();
    public int Count => items == null ? 0 : items.Count;

    public DecorItemDefinition Get(int idx)
    {
        if (items == null || items.Count == 0) return null;
        idx = Mathf.Clamp(idx, 0, items.Count - 1);
        return items[idx];
    }
}
