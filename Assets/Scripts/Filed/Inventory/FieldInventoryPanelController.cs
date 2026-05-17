using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FieldInventoryPanelController : BasePanel
{
    [Header("Item List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private FieldInventoryItemView itemViewPrefab;
    [SerializeField] private int slotCount = 20;

    [Header("Description")]
    [SerializeField] private CanvasGroup descriptionPanel;
    [SerializeField] private TMP_Text selectedItemNameText;
    [SerializeField] private TMP_Text selectedItemDescriptionText;

    private readonly List<FieldInventoryItemView> spawnedItems = new();

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
        //还需要一个清楚描述的东西
        ClearDescription();
        HideDescription();
    }
    public override void Show()
    {
        Refresh();
        base.Show();
    }
    public void Refresh()
    {
        ClearItems();
        ClearDescription();
        if (contentRoot == null || itemViewPrefab == null)
            return;

        List<KeyValuePair<ItemData, int>> itemStacks = new List<KeyValuePair<ItemData, int>>();

        foreach (var pair in InventoryRuntimeState.Items)
        {
            ItemData item = pair.Key;
            int count = pair.Value;

            if (item == null || count <= 0)
                continue;

            itemStacks.Add(pair);
        }

        int requiredSlotCount = Mathf.Max(slotCount, itemStacks.Count);

        for (int i = 0; i < requiredSlotCount; i++)
        {
            FieldInventoryItemView view = Instantiate(itemViewPrefab, contentRoot);

            if (i < itemStacks.Count)
            {
                var stack = itemStacks[i];
                view.Bind(stack.Key, stack.Value, HandleSelected);
            }
            else
            {
                view.BindEmpty(HandleSelected);
            }

            spawnedItems.Add(view);
        }
    }
    private void HandleSelected(ItemData item)
    {
        if (descriptionPanel == null)
            return;

        if (item == null)
        {
            HideDescription();
            return;
        }
        
        ShowDescription(item);
    }
    private void ShowDescription(ItemData item)
    {
        if(descriptionPanel != null)
        {
            descriptionPanel.alpha = 1f;
            descriptionPanel.interactable = true;
            descriptionPanel.blocksRaycasts = true;
        }

        selectedItemNameText.text = item.itemName;
        selectedItemDescriptionText.text = item.description;
    }
    private void HideDescription()
    {
        if (descriptionPanel != null)
        {
            descriptionPanel.alpha = 0f;
            descriptionPanel.interactable = false;
            descriptionPanel.blocksRaycasts = false;
        }
        ClearDescription();
    }
    public void ClearDescription()
    {
        if (selectedItemNameText != null)
            selectedItemNameText.text = "";
        if (selectedItemDescriptionText != null)
            selectedItemDescriptionText.text = "";
    }
    public void ClearItems()
    {
        for(int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i]!=null)
                Destroy(spawnedItems[i].gameObject);
        }
        spawnedItems.Clear();
    }
}
