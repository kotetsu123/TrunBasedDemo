using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldInventoryInputController : MonoBehaviour
{
    [SerializeField] private FieldInventoryPanelController inventorypanel;
    [SerializeField] private FieldStatusPanelController statusPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private bool isOpen;
    private FieldSystemMenuController systemMenu;
    private int lastEscCloseFrame = -1;

    public bool IsOpen => systemMenu != null
        ? systemMenu.IsOpen && systemMenu.CurrentPage == FieldSystemMenuController.Page.Item
        : isOpen;

    public void BindSystemMenu(FieldSystemMenuController menu) { systemMenu = menu; }

    private void Update()
    {
        if (systemMenu != null)
        {
            if (Input.GetKeyDown(toggleKey)) systemMenu.ToggleItems();
            return;
        }
        if (statusPanel != null && (statusPanel.IsShowing || statusPanel.ClosedThisFrame))
            return;

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(closeKey))
        {
            TryCloseByEsc();
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

    public bool TryCloseByEsc()
    {
        if (systemMenu != null) return systemMenu.TryCloseByEsc();
        // ESC 可能会被 Inventory 和 EscMenu 两个输入脚本在同一帧检测到。
        // 这里记录关闭背包的帧数，让 EscMenu 知道这一帧的 ESC 已经被背包消费了。
        if (!isOpen)
            return lastEscCloseFrame == Time.frameCount;

        if (inventorypanel != null && inventorypanel.TryCancelTargetSelection())
            return true;

        CloseInventory();
        lastEscCloseFrame = Time.frameCount;
        return true;
    }

    public void CloseInventory()
    {
        if (systemMenu != null) { systemMenu.Close(); return; }
        isOpen = false;

        if (inventorypanel == null)
            return;

        inventorypanel.Hide();
        FieldPauseState.SetPaused(false);
    }
}
