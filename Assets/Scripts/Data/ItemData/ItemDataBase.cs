using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Item Database")]
public class ItemDataBase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    // Save data only stores itemId, so loading needs this lookup to restore ItemData.
    public ItemData FindById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null)
                continue;

            // item.name / itemName fallback keeps old ItemData assets loadable even before itemId is serialized.
            if (item.itemId == itemId || item.name == itemId || item.itemName == itemId)
                return item;
        }

        Debug.LogWarning($"[ItemDataBase] Item not found. itemId={itemId}");
        return null;
    }
}