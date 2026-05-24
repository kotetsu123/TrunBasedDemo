using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySaveLoadDebugTester : MonoBehaviour
{
    [SerializeField] private ItemDataBase itemDatabase;

    [ContextMenu("Test Inventory Save Load")]
    private void TestInventorySaveLoad()
    {
        InventorySaveData saveData = InventoryRuntimeState.ToSaveData();

        Debug.Log($"[InventorySaveLoadTest] Saved slots count={saveData.slots.Count}");

        InventoryRuntimeState.Clear();

        Debug.Log($"[InventorySaveLoadTest] After Clear slots count={InventoryRuntimeState.Slots.Count}");

        InventoryRuntimeState.LoadFromSaveData(saveData, itemDatabase);

        Debug.Log($"[InventorySaveLoadTest] After Load slots count={InventoryRuntimeState.Slots.Count}");

        for (int i = 0; i < InventoryRuntimeState.Slots.Count; i++)
        {
            InventorySlotState slot = InventoryRuntimeState.Slots[i];

            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"[InventorySaveLoadTest] slot {i}: Empty");
                continue;
            }

            Debug.Log($"[InventorySaveLoadTest] slot {i}: {slot.item.itemId} x{slot.count}");
        }
    }
}
