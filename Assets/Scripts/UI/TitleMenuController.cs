using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private string fildSceneName = "FildScene";
    [SerializeField] private GameObject settingPanel;

    [Header("Save / Load")]
    [SerializeField] private Button loadGameButton;
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;

    private void Start()
    {
        RefreshLoadButtonState();
    }

    private void OnEnable()
    {
        RefreshLoadButtonState();
    }

    public void OnNewGameClicked()
    {
        // New Game should start from clean runtime data, not from a previous play session in memory.
        PartyRuntimeState.Clear();
        InventoryRuntimeState.Clear();
        FieldBattleContext.ClearAll();

        SceneManager.LoadScene(fildSceneName);
    }

    public void OnLoadGameClicked()
    {
        if (!SaveSystem.HasSaveFile())
        {
            RefreshLoadButtonState();
            return;
        }

        if (!SaveSystem.Load(itemDataBase, characterDataBase))
            return;

        // Load already restores cleared spawn IDs, so only transient battle-return data should be reset.
        FieldBattleContext.ClearReturnData();

        SceneManager.LoadScene(fildSceneName);
    }

    public void OnSettingClicked()
    {
        settingPanel.SetActive(true);
    }
    public void OnQuitGameClicked()
    {
        Application.Quit();
    }
    public void OnSettingCloseClicked()
    {
        settingPanel?.SetActive(false);
    }

    private void RefreshLoadButtonState()
    {
        // Disable Load when no save file exists, so the title menu cannot start an invalid load flow.
        if (loadGameButton != null)
            loadGameButton.interactable = SaveSystem.HasSaveFile();
    }

}
