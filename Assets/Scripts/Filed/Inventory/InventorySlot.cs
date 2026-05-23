using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour,IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("[InventorySlot] OnDrop Called");

        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem=dropped.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            Debug.LogWarning("[InventorySlot] DraggableItem not found.");
            return;
        }
        //当InventorySlot 所在的ui格子，就是drop的目标slot
        FieldInventoryItemView targetView = GetComponentInParent<FieldInventoryItemView>();

        if (targetView == null)
            return;

        int fromIndex = draggableItem.SourceSlotIndex;
        int toIndex = targetView.SlotIndex;

        if (fromIndex < 0 || toIndex < 0)
        {
            Debug.LogWarning($"[InventorySlot] Invalid slot index. fromIndex={fromIndex}, toIndex={toIndex}");
            return;
        }
        if (InventoryRuntimeState.SwapSlots(fromIndex, toIndex))
        {
            Debug.Log($"[InventorySlot] Swapped slot {fromIndex} with slot {toIndex}");

            FieldInventoryPanelController panel=GetComponentInParent<FieldInventoryPanelController>();
            panel?.Refresh();

            return;
        }
        Debug.Log($"[InventorySlot] Drop from slot{fromIndex}to slot {toIndex}");

        
            draggableItem.parentAfterDrag = transform;
    }

   
}
