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

        // Inventory gets the first chance to consume ESC, including the frame where it just closed itself.
        if (inventoryInputController != null && inventoryInputController.TryCloseByEsc())
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
