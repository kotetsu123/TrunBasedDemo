using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InitialItemStack
{
    public ItemData item;
    public int count;
}

[System.Serializable]
public class InventorySlotState
{
    public ItemData item;
    public int count;

    // 一个背包格子的运行时状态。item 为 null 或 count <= 0 时，这个格子就当作空格。
    public bool IsEmpty => item == null || count <= 0;

    public void Set(ItemData newItem, int newCount)
    {
        item = newItem;
        count = newCount;
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }
}

public static class InventoryRuntimeState
{
    public const int DefaultCapacity = 20;

    // 背包真正保存的是一排 slot，而不是 Dictionary。
    // 这样 UI、拖拽、保存/读取都可以依赖固定的格子顺序。
    private static readonly List<InventorySlotState> slots = new();

    public static IReadOnlyList<InventorySlotState> Slots => slots;
    public static int Capacity => slots.Count;

    public static bool IsInitialized { get; private set; }

    public static void InitializeIfEmpty(IEnumerable<InitialItemStack> initialItems, int capacity = DefaultCapacity)
    {
        if (IsInitialized)
            return;

        Clear();

        // 先生成固定数量的空格子，再把初始道具放进去。
        // 这样就算只有 1 个道具，背包也仍然会显示 20 个 slot。
        EnsureCapacity(capacity);

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

        // 第一版规则：同一种道具会优先堆叠到已有格子里。
        InventorySlotState stackSlot = FindSlot(item);
        if (stackSlot != null)
        {
            stackSlot.count += count;
            return;
        }

        // 如果背包里还没有这种道具，就放到第一个空格子。
        InventorySlotState emptySlot = FindEmptySlot();
        if (emptySlot == null)
        {
            // 现在还没有“背包已满”的正式流程，所以先扩容并给 warning。
            // 之后做掉落/商店时，可以把这里改成 AddItem 失败并提示玩家。
            emptySlot = new InventorySlotState();
            slots.Add(emptySlot);
            Debug.LogWarning("[InventoryRuntimeState] Inventory capacity was full, expanded by one slot.");
        }

        emptySlot.Set(item, count);
    }

    public static bool HasItem(ItemData item)
    {
        return GetItemCount(item) > 0;
    }

    public static bool ConsumeItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0)
            return false;

        InventorySlotState slot = FindSlot(item);
        if (slot == null)
            return false;

        if (slot.count < count)
            return false;

        slot.count -= count;

        // 数量用完后不移除 slot，只清空内容。slot index 要保留下来给 UI/拖拽使用。
        if (slot.count <= 0)
            slot.Clear();

        return true;
    }

    public static List<ItemData> GetAvailableItems()
    {
        List<ItemData> result = new List<ItemData>();

        foreach(var slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                continue;
            if (result.Contains(slot.item))
                continue;

            result.Add(slot.item);
        }
        return result;
    }

    public static int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;
        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                continue;
            if (slot.item == item)
                total += slot.count;
        }

        return total;
    }

    public static bool CanUseItem(ItemData item)
    {
        return GetItemCount(item) > 0;
    }

    public static bool SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
            return true;
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex))
            return false;

        // 之后拖拽道具时，只需要交换两个 slot 的运行时数据，再刷新 UI。
        (slots[fromIndex], slots[toIndex]) = (slots[toIndex], slots[fromIndex]);
        return true;
    }

    public static void Clear()
    {
        slots.Clear();
        IsInitialized = false;
    }

    private static void EnsureCapacity(int capacity)
    {
        int targetCapacity = Mathf.Max(0, capacity);

        // 确保 slots 至少有 targetCapacity 个格子。
        // 这里不会缩小背包，只负责补足缺少的空 slot。
        while (slots.Count < targetCapacity)
            slots.Add(new InventorySlotState());
    }

    private static InventorySlotState FindSlot(ItemData item)
    {
        // 找到已经持有同一种 ItemData 的格子，用于堆叠或消耗。
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotState slot = slots[i];
            if (slot == null || slot.IsEmpty)
                continue;
            if (slot.item == item)
                return slot;
        }

        return null;
    }

    private static InventorySlotState FindEmptySlot()
    {
        // 找第一个空格子。空格子的 slot 本身要保留，只清空 item/count。
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotState slot = slots[i];
            if (slot == null)
            {
                slot = new InventorySlotState();
                slots[i] = slot;
            }
            if (slot.IsEmpty)
                return slot;
        }

        return null;
    }

    private static bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < slots.Count;
    }
}
