using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InitialItemStack
{
    public ItemData item;
    public int count;
}

public static class InventoryRuntimeState
{
    private static readonly Dictionary<ItemData, int> items = new();

    public static IReadOnlyDictionary<ItemData, int> Items => items;

    public static bool IsInitialized { get; private set; }
    public static void InitializeIfEmpty(IEnumerable<InitialItemStack> initialItems)
    {
        if (IsInitialized)
            return;

        Clear();
        if(initialItems!=null)
        {
            foreach (var stack in initialItems)
            {
                if (stack == null)
                    continue;     
                
                AddItem(stack.item, stack.count);
            }
        }    
        IsInitialized = true;
    }

    public static void AddItem(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return;

        if (!items.ContainsKey(item))
            items[item] = 0;

        items[item] += count;
    }

    public static bool HasItem(ItemData item)
    {
        return item != null && items.TryGetValue(item, out int count) && count > 0;
    }
   
    public static bool ConsumeItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return false;

        if (!items.TryGetValue(item, out int current))
            return false;

        if (current < count)
            return false;

        items[item] = current - count;

        if (items[item] <= 0)
            items.Remove(item);

        return true;

    }
    public static List<ItemData> GetAvailableItems()
    {
        List<ItemData> result = new List<ItemData>();

       foreach(var pair in items)
        {
            if (pair.Key == null)
                continue;
            if (pair.Value <= 0)
                continue;

            result.Add(pair.Key);
        }
        return result;
    }
    public static int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        return items.TryGetValue(item, out int count) ? count : 0;
    }

    public static bool CanUseItem(ItemData item)
    {
        return GetItemCount(item) > 0;
    }

   
    public static void Clear()
    {
        items.Clear();
        IsInitialized = false;
    }
}
