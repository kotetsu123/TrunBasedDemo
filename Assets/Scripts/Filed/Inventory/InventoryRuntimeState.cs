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

    // A slot is empty when it has no item or its count is zero.
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

    // Runtime inventory is stored as ordered slots, not a Dictionary.
    // This lets UI, drag-and-drop, and save/load preserve exact slot positions.
    private static readonly List<InventorySlotState> slots = new();

    public static IReadOnlyList<InventorySlotState> Slots => slots;
    public static int Capacity => slots.Count;

    public static bool IsInitialized { get; private set; }

    public static void InitializeIfEmpty(IEnumerable<InitialItemStack> initialItems, int capacity = DefaultCapacity)
    {
        if (IsInitialized)
            return;

        Clear();

        // Create fixed empty slots first, then place initial items into them.
        EnsureCapacity(capacity);

        if (initialItems != null)
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

        // First version rule: same ItemData stacks into the existing slot.
        InventorySlotState stackSlot = FindSlot(item);
        if (stackSlot != null)
        {
            stackSlot.count += count;
            return;
        }

        // If this item does not exist yet, put it into the first empty slot.
        InventorySlotState emptySlot = FindEmptySlot();
        if (emptySlot == null)
        {
            // There is no formal inventory-full flow yet, so expand for now and warn.
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

        // Keep the slot index stable; only clear its content when the item is used up.
        if (slot.count <= 0)
            slot.Clear();

        return true;
    }

    public static List<ItemData> GetAvailableItems()
    {
        List<ItemData> result = new List<ItemData>();

        foreach (var slot in slots)
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

        // Drag-and-drop only needs to swap runtime slot data, then refresh UI.
        (slots[fromIndex], slots[toIndex]) = (slots[toIndex], slots[fromIndex]);
        return true;
    }

    public static InventorySaveData ToSaveData()
    {
        InventorySaveData saveData = new InventorySaveData();

        // Save every runtime slot, including empty slots, to preserve inventory layout.
        foreach (var slot in slots)
        {
            InventorySlotSaveData slotSaveData = new InventorySlotSaveData();

            if (slot != null && !slot.IsEmpty)
            {
                slotSaveData.itemId = slot.item.itemId;
                slotSaveData.count = slot.count;
            }

            saveData.slots.Add(slotSaveData);
        }

        return saveData;
    }

    public static void LoadFromSaveData(InventorySaveData saveData, ItemDataBase itemDatabase)
    {
        Clear();

        if (saveData == null || saveData.slots == null)
        {
            // No save data means an empty default inventory.
            EnsureCapacity(DefaultCapacity);
            IsInitialized = true;
            return;
        }

        // Rebuild the runtime slots in the exact same order as the saved slots.
        for (int i = 0; i < saveData.slots.Count; i++)
        {
            InventorySlotSaveData slotSaveData = saveData.slots[i];
            InventorySlotState slot = new InventorySlotState();

            if (slotSaveData != null && slotSaveData.count > 0)
            {
                ItemData item = itemDatabase != null
                    ? itemDatabase.FindById(slotSaveData.itemId)
                    : null;

                if (item != null)
                {
                    // Fill this runtime slot with restored ItemData/count.
                    slot.Set(item, slotSaveData.count);
                }
                else
                {
                    Debug.LogWarning($"[InventoryRuntimeState] Failed to load item. itemId={slotSaveData.itemId}");
                }
            }

            // Always add the slot, even when empty, so the saved layout is preserved.
            slots.Add(slot);
        }

        // If an old save has fewer slots than the current default capacity, fill the rest.
        EnsureCapacity(DefaultCapacity);
        IsInitialized = true;
    }

    public static void Clear()
    {
        slots.Clear();
        IsInitialized = false;
    }

    private static void EnsureCapacity(int capacity)
    {
        int targetCapacity = Mathf.Max(0, capacity);

        // Only adds missing slots; it never shrinks the inventory.
        while (slots.Count < targetCapacity)
            slots.Add(new InventorySlotState());
    }

    private static InventorySlotState FindSlot(ItemData item)
    {
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