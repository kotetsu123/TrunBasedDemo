using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldInventoryInputController : MonoBehaviour
{
    [SerializeField] private FieldInventoryPanelController inventorypanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private bool isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
        if (isOpen && inventorypanel != null && !inventorypanel.IsSelectingTarget && Input.GetKeyDown(closeKey))
        {
            CloseInventory();
        }
    }
    private void ToggleInventory()
    {
        if (inventorypanel == null)
            return;

        isOpen = !isOpen;

        if (isOpen)
        {
            inventorypanel.Show();
            FieldPauseState.SetPaused(true);
        }
           
        else
        {
            CloseInventory();
        }
           
    }
    private void CloseInventory()
    {
        if (inventorypanel == null)
            return;

        isOpen = false;
        inventorypanel.Hide();
        FieldPauseState.SetPaused(false);
    }
}
