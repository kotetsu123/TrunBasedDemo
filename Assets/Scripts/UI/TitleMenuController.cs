using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private GameObject settingPanel;
    public void OnNewGameClicked()
    {
        SceneManager.LoadScene(battleSceneName);
    }
    public void OnLoadGameClicked()
    {

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

}
