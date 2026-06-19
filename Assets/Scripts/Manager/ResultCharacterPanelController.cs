using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultCharacterPanelController : BasePanel
{
    [SerializeField] private BattleEndPanelController endPanel;
    [SerializeField] private GameObject buttonRoot;
    [SerializeField] private string titleMenuName = "TitleScene";
    //settlepanel canvasGroup
   
    [SerializeField] private CharacterResultItemView[] items = new CharacterResultItemView[4];

    [SerializeField] private LevelUpPopController levelUpPopup;

    [SerializeField] private float returnFieldDelay = 2.0f;
    [SerializeField] private string fieldSceneName = "FildScene";

    [Header("Save / Load")]
    [SerializeField] private Button loadGameButton;
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private CharacterDataBase characterDataBase;

    [Header("Reward")]
    [SerializeField] private RewardPopController rewardPopup;

    protected override void Awake()
    {
        base.Awake();
        // Result panel must start fully hidden. A tweened Hide() can leave the panel visible for a short moment
        // when entering a fresh BattleScene after Escape/Run.
        HideImmediate();
    }
    private void OnEnable()
    {
        if (endPanel != null)
            endPanel.OnClosed += HandleEndPanelClosed;

        RefreshLoadButtonState();
    }
    private void OnDisable()
    {
        if (endPanel != null)
            endPanel.OnClosed -= HandleEndPanelClosed;
    }
    public void Show(IReadOnlyList<CharacterResultSnapshot> partySnapShots,
        BattleResult result,
        EncounterRewardResult rewardResult)
    {
        bool isVictory = result == BattleResult.Win;
        bool isEscape= result== BattleResult.Escape;
        bool isLose=result== BattleResult.Lose;

        if (buttonRoot != null)
            buttonRoot.SetActive(isLose);

        Debug.Log($"[SettlePanel] Show snapshots={(partySnapShots == null ? "NULL" : partySnapShots.Count.ToString())}");
        if (partySnapShots != null)
        {
            for (int i = 0; i < partySnapShots.Count; i++)
                Debug.Log($"[SettlePanel] snap[{i}] name={partySnapShots[i]?.Name} hp={partySnapShots[i]?.hp}/{partySnapShots[i]?.maxhp}");
        }
        if (isVictory||isEscape)
        {          
            // Win/Escape: show the normal character result items.
            for (int i = 0; i < items.Length; i++)
            {
                if (partySnapShots != null && i < partySnapShots.Count)
                {
                    items[i].gameObject.SetActive(true);
                    items[i].Bind(partySnapShots[i]);
                }
                else
                {
                    items[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {        
            // Lose: show retry/title buttons and hide character result items.
            for(int i = 0; i < items.Length; i++)
            {
                items[i].gameObject.SetActive(false);
            }
            buttonRoot.SetActive(true);
            
        }
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
       
    }

    private IEnumerator PlayLevelUpPopUps(List<LevelUpResult> results)
    {
        if (results == null || results.Count == 0)
            yield break;

        foreach (var result in results)
        {
            if (!result.DidLevelUp)
                continue;

            if (levelUpPopup != null)
            {
                levelUpPopup.Play(result);

                yield return new WaitForSeconds(levelUpPopup.GetTotalDuration());
            }
        }
    }

    private IEnumerator PlayWinResultSequence(EncounterRewardResult rewardResult)
    {
        if (rewardPopup != null && rewardPopup.HasReward(rewardResult))
        {
            rewardPopup.Play(rewardResult);
            yield return new WaitForSeconds(rewardPopup.GetTotalDuration());
        }

        yield return PlayLevelUpPopUps(BattleManager.Instance.LastLevelUpResults.ToList());

        yield return new WaitForSeconds(returnFieldDelay);

        ReturnToField();
    }
   
    private void ReturnToField()
    {
        if (FieldBattleContext.HasFieldReturnData)
        {
            SceneManager.LoadScene(FieldBattleContext.LastFieldSceneName);
        }
        else
        {
            SceneManager.LoadScene(fieldSceneName);
        }
    }
    private void HandleEndPanelClosed(BattleResultPayload payload)
    {
        if (payload == null) return;    

        Show(payload.PartySnapshots, payload.Result, payload.RewardResult);
        if (payload.Result == BattleResult.Win)
        {
            StartCoroutine(PlayWinResultSequence(payload.RewardResult));
            return;
        }
        if (payload.Result == BattleResult.Escape)
        {
            StartCoroutine(AutoReturnFieldAfterDelay());
        }
    }
    public void OnClickRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnClickBackToTile()
    {
        SceneManager.LoadScene(titleMenuName);
    }

    public void OnClickLoadGame()
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

        SceneManager.LoadScene(SaveSystem.GetLoadedFieldSceneNameOrDefault(fieldSceneName));
    }

    private void RefreshLoadButtonState()
    {
        // Keep the battle result Load button disabled until a real save file exists.
        if (loadGameButton != null)
            loadGameButton.interactable = SaveSystem.HasSaveFile();
    }

    private IEnumerator AutoReturnFieldAfterDelay()
    {
        yield return new WaitForSeconds(returnFieldDelay);

        ReturnToField();
    }
}
