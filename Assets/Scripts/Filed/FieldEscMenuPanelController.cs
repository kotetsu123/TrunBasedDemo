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

    [Header("Load Data")]
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private string fallbackFieldSceneName = "FildScene";

    [Header("Field UI")]
    [SerializeField] private FieldInventoryPanelController inventoryPanel;
    [SerializeField] private FieldPartyHudController partyHudController;

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
    }

    private void OnDestroy()
    {
        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnClickSave);
        if (loadButton != null)
            loadButton.onClick.RemoveListener(OnClickLoad);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickClose);
    }

    public override void Show()
    {
        RefreshLoadButtonState();
        ClearMessage();
        base.Show();
    }

    public void OnClickSave()
    {
        bool saved = SaveSystem.Save();
        SetMessage(saved ? "Saved" : "Save failed");
        RefreshLoadButtonState();
    }

    public void OnClickLoad()
    {
        if (!SaveSystem.HasSaveFile())
        {
            SetMessage("No save file");
            RefreshLoadButtonState();
            return;
        }

        if (!SaveSystem.Load(itemDataBase, characterDataBase))
        {
            SetMessage("Load failed");
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
        SetMessage("Loaded");
    }

    public void OnClickClose()
    {
        Hide();
        FieldPauseState.SetPaused(false);
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
}
