using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FieldEscMenuPanelController : BasePanel
{
    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button statusButton;

    [Header("Load Data")]
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private string fallbackFieldSceneName = "FildScene";

    [Header("Field UI")]
    [SerializeField] private FieldInventoryPanelController inventoryPanel;
    [SerializeField] private FieldPartyHudController partyHudController;
    [SerializeField] private FieldToastController toastController;
    [SerializeField] private FieldStatusPanelController statusPanel;
    [SerializeField] private FieldSystemMenuController systemMenu;

    [Header("Message")]
    [SerializeField] private TMP_Text messageText;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
        ClearMessage();

        if (saveButton != null)
            saveButton.onClick.AddListener(OnClickSave);
        if (loadButton != null)
            loadButton.onClick.AddListener(OnClickLoad);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
        if (statusButton != null)
            statusButton.onClick.AddListener(OnClickStatus);
    }

    private void OnDestroy()
    {
        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnClickSave);
        if (loadButton != null)
            loadButton.onClick.RemoveListener(OnClickLoad);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);
        if (statusButton != null)
            statusButton.onClick.RemoveListener(OnClickStatus);
    }

    public override void Show()
    {
        RefreshLoadButtonState();
        ClearMessage();
        base.Show();
    }

    public void OnClickSave()
    {
        if (systemMenu != null) { systemMenu.OpenSaveLoadFromMenu(this, true); return; }
        bool saved = SaveSystem.Save();
        string message = saved ? "Saved" : "Save failed";
        SetMessage(message);
        ShowToast(message);
        RefreshLoadButtonState();
    }

    public void OnClickLoad()
    {
        if (systemMenu != null) { systemMenu.OpenSaveLoadFromMenu(this, false); return; }
        if (!SaveSystem.HasSaveFile())
        {
            const string message = "No save file";
            SetMessage(message);
            ShowToast(message);
            RefreshLoadButtonState();
            return;
        }

        if (!SaveSystem.Load(itemDataBase, characterDataBase))
        {
            const string message = "Load failed";
            SetMessage(message);
            ShowToast(message);
            return;
        }

        // If the save belongs to another Field scene, switch scenes after loading runtime data.
        string targetSceneName = SaveSystem.GetLoadedFieldSceneNameOrDefault(fallbackFieldSceneName);
        if (!string.IsNullOrWhiteSpace(targetSceneName) && targetSceneName != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        // Loading in the same Field scene already applies position through FieldSaveContext.
        inventoryPanel?.Refresh();
        partyHudController?.Refresh();
        statusPanel?.Refresh();
        const string loadedMessage = "Loaded";
        SetMessage(loadedMessage);
        ShowToast(loadedMessage);
    }

    public void OnClickClose()
    {
        Hide();
        FieldPauseState.SetPaused(false);
    }

    public void OnClickStatus()
    {
        statusPanel?.OpenFromMenu(this);
    }

    private void RefreshLoadButtonState()
    {
        if (loadButton != null)
            loadButton.interactable = SaveSystem.HasSaveFile();
    }

    private void ClearMessage()
    {
        SetMessage("");
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    private void ShowToast(string message)
    {
        if (toastController == null)
            toastController = FieldToastController.Current;

        toastController?.ShowMessage(message);
    }
}
