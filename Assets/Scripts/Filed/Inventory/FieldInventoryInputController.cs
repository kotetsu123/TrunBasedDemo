using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldInventoryInputController : MonoBehaviour
{
    [SerializeField] private FieldInventoryPanelController inventorypanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    private bool isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
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
            inventorypanel.Hide();
            FieldPauseState.SetPaused(false);
        }
           
    }
}
