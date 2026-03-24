using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private BattleManager battle;

    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.99f;

    private void Awake()
    {
        HideImmediate();
    }
    public void Show()
    {
        if (itemPanel != null)
        {
           canvasGroup.alpha= 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }
    public void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void OnItemButtonClicked(ItemData item)
    {
        if (battle == null || item == null) return;

        battle.HandleItemSelected(item);
        HideImmediate();
    }
}
