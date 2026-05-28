using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveSystemDebugTester : MonoBehaviour
{
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private FieldInventoryPanelController inventoryPanel;
    [SerializeField] private FieldPartyHudController partyHudController;

    [ContextMenu("Save Game")]
    private void SaveGame()
    {
        SaveSystem.Save();
    }

    [ContextMenu("Load Game")]
    private void LoadGame()
    {
        if (!SaveSystem.Load(itemDataBase, characterDataBase))
            return;

        // Load changes runtime data. Refresh currently visible field UI so it matches the loaded state.
        inventoryPanel?.Refresh();
        partyHudController?.Refresh();
    }
}
