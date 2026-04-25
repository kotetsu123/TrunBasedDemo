using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultCharacterPanelController : BasePanel
{
    [SerializeField] private BattleEndPanelController endPanel;
    [SerializeField] private GameObject buttonRoot;
    [SerializeField] private string titleMenuName = "TitleScene";
    //settlepanel canvasGroup
   
    [SerializeField] private CharacterResultItemView[] items = new CharacterResultItemView[4];

    [SerializeField]private LevelUpPopController levelUpPopup;  

    protected override void Awake()
    {
        base.Awake();
        Hide();
    }
    private void OnEnable()
    {
        if (endPanel != null)
            endPanel.OnClosed += HandleEndPanelClosed;
    }
    private void OnDisable()
    {
        if (endPanel != null)
            endPanel.OnClosed -= HandleEndPanelClosed;
    }
    public  void Show(IReadOnlyList<CharacterResultSnapshot> partySnapShots,BattleResult result)
    {
        bool isVictory = result == BattleResult.Win;
        if(buttonRoot!=null)
       buttonRoot.SetActive(!isVictory);

        Debug.Log($"[SettlePanel] Show snapshots={(partySnapShots == null ? "NULL" : partySnapShots.Count.ToString())}");
        if (partySnapShots != null)
        {
            for (int i = 0; i < partySnapShots.Count; i++)
                Debug.Log($"[SettlePanel] snap[{i}] name={partySnapShots[i]?.Name} hp={partySnapShots[i]?.hp}/{partySnapShots[i]?.maxhp}");
        }
        if (isVictory)
        {          
            //胜利：正常显示角色结算
            for (int i = 0; i < items.Length; i++)
            {
                if (i < partySnapShots.Count)
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
            //失败，显示buttonRoot 当中的两个按钮，隐藏角色结算
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

                yield return new WaitForSeconds( levelUpPopup.GetTotalDuration());
            }
        }
    }

    private void HandleEndPanelClosed(BattleResultPayload payload)
    {
        if (payload == null) return;    

        Show(payload.PartySnapshots,payload.Result);   
        if(payload.Result==BattleResult.Win)
        StartCoroutine(PlayLevelUpPopUps(BattleManager.Instance.LastLevelUpResults.ToList()));
    }
    public void OnClickRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnClickBackToTile()
    {
        SceneManager.LoadScene(titleMenuName);
    }
}
