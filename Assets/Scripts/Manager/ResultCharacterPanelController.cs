using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultCharacterPanelController : MonoBehaviour
{
    [SerializeField] private BattleEndPanelController endPanel;
    //settlepanel canvasGroup
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private CharacterResultItemView[] items = new CharacterResultItemView[4];

    private void Awake()
    {
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
    public void Show(IReadOnlyList<CharacterResultSnapshot> partySnapShots)
    {
        Debug.Log($"[SettlePanel] Show snapshots={(partySnapShots == null ? "NULL" : partySnapShots.Count.ToString())}");
        if (partySnapShots != null)
        {
            for (int i = 0; i < partySnapShots.Count; i++)
                Debug.Log($"[SettlePanel] snap[{i}] name={partySnapShots[i]?.Name} hp={partySnapShots[i]?.hp}/{partySnapShots[i]?.maxhp}");
        }
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
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
    public void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable= false;

    }
    private void HandleEndPanelClosed(BattleResultPayload payload)
    {
        Show(payload.PartySnapshots);
    }
}
