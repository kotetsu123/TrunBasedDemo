using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class FieldSystemMenuController : BasePanel
{
    public enum Page { Status, Item, SaveLoad }

    [SerializeField] private FieldStatusPanelController statusPanel;
    [SerializeField] private FieldInventoryPanelController inventoryPanel;
    [SerializeField] private SaveLoadPanelController saveLoadPanel;
    [SerializeField] private Toggle statusTab;
    [SerializeField] private Toggle itemTab;
    [SerializeField] private Toggle saveLoadTab;
    [SerializeField] private Button closeButton;

    private FieldEscMenuPanelController returnMenu;
    private bool previousPause;
    private bool isShowing;
    private int lastCloseFrame = -1;
    private int lastBackFrame = -1;

    public override bool IsOpen => isShowing;
    public Page CurrentPage { get; private set; }
    public bool ClosedThisFrame => lastCloseFrame == Time.frameCount;

    protected override void Awake()
    {
        base.Awake();
        statusPanel?.BindSystemMenu(this);
        inventoryPanel?.GetComponent<FieldInventoryInputController>()?.BindSystemMenu(this);
        saveLoadPanel?.BindSystemMenu(this);
        statusTab.onValueChanged.AddListener(OnStatusTabChanged);
        itemTab.onValueChanged.AddListener(OnItemTabChanged);
        if (saveLoadTab != null) saveLoadTab.onValueChanged.AddListener(OnSaveLoadTabChanged);
        closeButton.onClick.AddListener(Close);
        HideImmediate();
    }

    private void OnDestroy()
    {
        statusTab.onValueChanged.RemoveListener(OnStatusTabChanged);
        itemTab.onValueChanged.RemoveListener(OnItemTabChanged);
        if (saveLoadTab != null) saveLoadTab.onValueChanged.RemoveListener(OnSaveLoadTabChanged);
        closeButton.onClick.RemoveListener(Close);
        currentTween?.Kill();
    }

    private void Update()
    {
        if (isShowing && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
            TryCloseByEsc();
    }

    public void OpenStatus() { Open(Page.Status); }
    public void OpenItems() { Open(Page.Item); }
    public void OpenSaveLoad() { Open(Page.SaveLoad); }
    public void OpenFromMenu(FieldEscMenuPanelController menu) { Open(Page.Status, menu); }
    public void OpenSaveLoadFromMenu(FieldEscMenuPanelController menu, bool save)
    {
        Open(Page.SaveLoad, menu, save ? SaveLoadPanelController.Mode.Save : SaveLoadPanelController.Mode.Load);
    }

    public void ToggleItems()
    {
        if (ClosedThisFrame || (saveLoadPanel != null && saveLoadPanel.IsConfirming)) return;
        if (isShowing && CurrentPage == Page.Item) Close();
        else OpenItems();
    }

    private void Open(Page page, FieldEscMenuPanelController sourceMenu = null,
        SaveLoadPanelController.Mode saveMode = SaveLoadPanelController.Mode.Save)
    {
        if (statusPanel == null || inventoryPanel == null || ClosedThisFrame ||
            TutorialPanelController.IsTutorialActive) return;
        if (saveLoadPanel != null && saveLoadPanel.IsConfirming) return;
        if (page == Page.SaveLoad && saveLoadPanel == null) return;

        if (!isShowing)
        {
            if (FieldPauseState.IsPaused && sourceMenu == null) return;
            previousPause = FieldPauseState.IsPaused;
            returnMenu = sourceMenu;
            returnMenu?.Hide();
            isShowing = true;
            FieldPauseState.SetPaused(true);
        }
        else if (CurrentPage == page) return;

        // Switching pages never releases the pause owned by this menu session.
        statusPanel.HideImmediate();
        inventoryPanel.HideImmediate();
        saveLoadPanel?.HideImmediate();
        CurrentPage = page;
        if (page == Page.Status) statusPanel.ShowPage();
        else if (page == Page.Item) inventoryPanel.Show();
        else saveLoadPanel.ShowPage(saveMode);
        statusTab.SetIsOnWithoutNotify(page == Page.Status);
        itemTab.SetIsOnWithoutNotify(page == Page.Item);
        if (saveLoadTab != null) saveLoadTab.SetIsOnWithoutNotify(page == Page.SaveLoad);
        base.Show();
    }

    public bool TryCloseByEsc()
    {
        if (ClosedThisFrame || lastBackFrame == Time.frameCount) return true;
        if (!isShowing) return false;
        lastBackFrame = Time.frameCount;
        if (CurrentPage == Page.SaveLoad && saveLoadPanel.TryCancelConfirmation()) return true;
        if (CurrentPage == Page.Item && inventoryPanel.TryCancelTargetSelection()) return true;
        Close();
        return true;
    }

    public void Close()
    {
        if (!isShowing) return;
        if (saveLoadPanel != null && saveLoadPanel.TryCancelConfirmation()) return;
        isShowing = false;
        lastCloseFrame = Time.frameCount;
        statusPanel.HideImmediate();
        inventoryPanel.HideImmediate();
        saveLoadPanel?.HideImmediate();
        base.HideImmediate();
        ClearSelection();
        var menu = returnMenu;
        returnMenu = null;
        if (menu != null)
        {
            menu.Show();
            FieldPauseState.SetPaused(true);
        }
        else FieldPauseState.SetPaused(previousPause);
    }

    private void ClearSelection()
    {
        var events = EventSystem.current;
        var selected = events != null ? events.currentSelectedGameObject : null;
        if (selected != null && (selected.transform.IsChildOf(transform) ||
            selected.transform.IsChildOf(statusPanel.transform) ||
            selected.transform.IsChildOf(inventoryPanel.transform) ||
            (saveLoadPanel != null && selected.transform.IsChildOf(saveLoadPanel.transform))))
            events.SetSelectedGameObject(null);
    }

    private void OnStatusTabChanged(bool selected) { if (selected) OpenStatus(); }
    private void OnItemTabChanged(bool selected) { if (selected) OpenItems(); }
    private void OnSaveLoadTabChanged(bool selected) { if (selected) OpenSaveLoad(); }
}
