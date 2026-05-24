using System.Collections.Generic;

[System.Serializable]
public class InventorySlotSaveData
{
    // Save itemId instead of a direct ItemData / ScriptableObject reference.
    public string itemId;
    public int count;
}

[System.Serializable]
public class InventorySaveData
{
    // Save every slot in order, including empty slots, so inventory layout can be restored.
    public List<InventorySlotSaveData> slots = new();
}
