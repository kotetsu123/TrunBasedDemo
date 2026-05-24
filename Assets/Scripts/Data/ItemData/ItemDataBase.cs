using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

[CreateAssetMenu(menuName ="Battle/Item Database")]
public class ItemDataBase :ScriptableObject
{
    [SerializeField] private List<ItemData> items = new();

    // 存档里只有 itemId，所以读取背包时需要通过数据库把 itemId 找回 ItemData
    public ItemData FindById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        for(int i = 0; i < items.Count; i++)
        {
            ItemData item= items[i];

            if (item == null)
                continue;

            if (item.itemId == itemId)
                return item;
        }
        Debug.LogWarning($"[ItemDataBase] Item not found. {itemId} 的 ItemData");
        return null;
    }
}
