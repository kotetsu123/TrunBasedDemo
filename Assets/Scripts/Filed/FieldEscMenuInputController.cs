using UnityEngine;

public class FieldEscMenuInputController : MonoBehaviour
{
    [SerializeField] private FieldEscMenuPanelController escPanel;
    [SerializeField] private FieldInventoryInputController inventoryInputController;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (escPanel == null)
            return;

        // Inventory owns Esc while it is open, so the system menu does not open on the same key press.
        if (inventoryInputController != null && inventoryInputController.IsOpen)
            return;

        if (escPanel.IsOpen)
        {
            escPanel.OnClickClose();
            return;
        }

        escPanel.Show();
        FieldPauseState.SetPaused(true);
    }
}
