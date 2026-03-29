using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class ItemPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform contentRoot;
    [SerializeField]private ItemItemView itemViewPrefab;
    [SerializeField] private BattleManager battle;
    //TODO: This should be a list of items, not just one item
    
    public Action<ItemData> OnItemSelected;

    private readonly List<ItemItemView> _spwanItems = new List<ItemItemView>();
    

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
        
           canvasGroup.alpha= 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        
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
   
    public void Refresh()
    {
        ClearItems();

        if (battle == null ||contentRoot==null||itemViewPrefab==null) return;
        var items = battle.GetAvailableItems();
        for(int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int count = battle.GetItemCount(item);

            var view = Instantiate(itemViewPrefab, contentRoot);
            view.Bind(item, count, HandleItemClicked);
            _spwanItems.Add(view);
        }
       
    }
    private void HandleItemClicked(ItemData item)
    {
        if (item == null) return;
        if (battle != null && !battle.CanUseItem(item))
            return;

        OnItemSelected?.Invoke(item);
        HideImmediate();
    }
    private void HandleItemCountChanged(ItemData item,int newCount)
    {
        Refresh();
    }
    private void ClearItems()
    {
        for(int i=0;i< _spwanItems.Count; i++)
        {
           if(_spwanItems[i]!=null)
            {
                Destroy(_spwanItems[i].gameObject);
            }
        }
        _spwanItems.Clear();
    }
}
