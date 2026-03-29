using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class ItemPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private BattleManager battle;
    //TODO: This should be a list of items, not just one item
    [SerializeField] private ItemData potionItem;
    [SerializeField] private TMPro.TMP_Text potionCountTxt;
    [SerializeField]private Button potionButton;

    public Action<ItemData> OnItemSelected;


    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.99f;

    private void OnEnable()
    {
        if (battle != null)
            battle.OnItemCountChanged += HandleItemCountChanged;
    }
    private void OnDisable()
    {
        if(battle!=null)
            battle.OnItemCountChanged -= HandleItemCountChanged;
    }
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
        Refresh();
    }
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void OnClickItem(ItemData item)
    {
        if (battle == null || item == null) return;

        // battle.HandleItemSelected(item);
        OnItemSelected?.Invoke(item);
        HideImmediate();
        
    }
    public void Refresh()
    {
        if (battle == null || potionItem == null) return;
        int count = battle.GetItemCount(potionItem);

        if (potionItem != null)
            potionCountTxt.text = $"x{ count}";
        if (potionButton != null)
        {
            potionButton.interactable = count > 0;
        }
    }
    private void HandleItemCountChanged(ItemData item,int newCount)
    {
        Refresh();
    }
}
