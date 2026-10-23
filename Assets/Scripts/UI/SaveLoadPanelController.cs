using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SaveLoadPanelController : BasePanel
{
    public enum Mode { Save, Load }
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private Toggle saveToggle;
    [SerializeField] private Toggle loadToggle;
    [SerializeField] private RectTransform rowRoot;
    [SerializeField] private SaveSlotView rowPrefab;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TMP_Text confirmationTitle;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    private readonly System.Collections.Generic.List<SaveSlotView> rows = new();
    private FieldSystemMenuController systemMenu;
    private int selectedSlot = -1;
    private int pendingSlot = -1;
    private bool titleMode;
    private bool showing;
    private bool loading;
    public Mode CurrentMode { get; private set; }
    public bool IsConfirming => pendingSlot >= 0;
    public override bool IsOpen => showing;

    protected override void Awake()
    {
        base.Awake();
        saveToggle.onValueChanged.AddListener(OnSaveMode);
        loadToggle.onValueChanged.AddListener(OnLoadMode);
        closeButton.onClick.AddListener(Close);
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(CancelConfirmation);
        HideImmediate();
    }
    private void OnDestroy()
    {
        saveToggle.onValueChanged.RemoveListener(OnSaveMode);
        loadToggle.onValueChanged.RemoveListener(OnLoadMode);
        closeButton.onClick.RemoveListener(Close);
        confirmButton.onClick.RemoveListener(Confirm);
        cancelButton.onClick.RemoveListener(CancelConfirmation);
        currentTween?.Kill();
    }
    private void Update()
    {
        if (showing && titleMode && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        { if (!TryCancelConfirmation()) Close(); }
    }
    public void BindSystemMenu(FieldSystemMenuController menu) { systemMenu = menu; }
    public void OpenFromTitle() { titleMode = true; ShowPage(Mode.Load); }
    public void ShowPage(Mode mode)
    {
        showing = true;
        loading = false;
        CurrentMode = titleMode ? Mode.Load : mode;
        closeButton.gameObject.SetActive(titleMode);
        saveToggle.gameObject.SetActive(!titleMode);
        saveToggle.SetIsOnWithoutNotify(CurrentMode == Mode.Save);
        loadToggle.SetIsOnWithoutNotify(CurrentMode == Mode.Load);
        selectedSlot = -1;
        CancelConfirmation();
        SetMessage("");
        Refresh();
        base.Show();
    }
    public override void HideImmediate()
    {
        showing = false;
        loading = false;
        CancelConfirmation();
        base.HideImmediate();
    }
    public void Refresh()
    {
        var slots = SaveSystem.GetSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            if (rows.Count <= i) rows.Add(Instantiate(rowPrefab, rowRoot));
            rows[i].Bind(slots[i], characterDataBase, CurrentMode == Mode.Save, selectedSlot == i, RequestSlot);
        }
    }
    public void RequestSlot(int slot)
    {
        if (!showing || loading || IsConfirming || slot < 0 || slot >= SaveSystem.SlotCount) return;
        var info = SaveSystem.GetSlotInfo(slot);
        selectedSlot = slot;
        Refresh();
        SetMessage("");
        if (CurrentMode == Mode.Load && !info.CanLoad) { SetMessage(info.Error ?? "This slot is empty.", true); return; }
        if (CurrentMode == Mode.Save && !info.Exists) { WriteSlot(slot); return; }
        pendingSlot = slot;
        confirmationTitle.text = CurrentMode == Mode.Save ? "Overwrite save?" : "Load save?";
        confirmationText.text = CurrentMode == Mode.Save ? $"Replace the progress in slot {slot + 1:00}?"
            : $"Load slot {slot + 1:00}" + (info.UsesBackup ? " from its backup" : "") + "? Unsaved progress will be lost.";
        confirmationRoot.SetActive(true);
        cancelButton.Select();
    }
    public bool TryCancelConfirmation()
    {
        if (!IsConfirming) return false;
        CancelConfirmation();
        return true;
    }
    public void CancelConfirmation() { pendingSlot = -1; confirmationRoot.SetActive(false); }
    private void Confirm()
    {
        if (!showing || pendingSlot < 0 || loading) return;
        int slot = pendingSlot;
        CancelConfirmation();
        if (CurrentMode == Mode.Save) { WriteSlot(slot); return; }
        if (!SaveSystem.LoadSlot(slot, itemDataBase, characterDataBase, false))
        { SetMessage(SaveSystem.LastError ?? "Load failed.", true); Refresh(); return; }
        loading = true;
        FieldPauseState.SetPaused(false);
        SceneManager.LoadScene(SaveSystem.GetLoadedFieldSceneNameOrDefault("FildScene"));
    }
    private void WriteSlot(int slot)
    {
        bool saved = SaveSystem.SaveSlot(slot);
        Refresh();
        SetMessage(saved ? $"Progress saved in slot {slot + 1:00}." : SaveSystem.LastError, !saved);
    }
    private void SwitchMode(Mode mode)
    {
        if (IsConfirming || loading) return;
        CurrentMode = mode;
        selectedSlot = -1;
        SetMessage("");
        Refresh();
    }
    private void OnSaveMode(bool on) { if (on && !titleMode) SwitchMode(Mode.Save); }
    private void OnLoadMode(bool on) { if (on) SwitchMode(Mode.Load); }
    private void SetMessage(string message, bool error = false)
    {
        messageText.text = message;
        messageText.color = error ? new Color(.95f, .48f, .43f) : new Color(.65f, .8f, .7f);
    }
    public void Close()
    {
        if (loading || TryCancelConfirmation()) return;
        if (systemMenu != null) systemMenu.Close(); else HideImmediate();
    }
}
