using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Button useItemButton;

    [Header("Party")]
    [SerializeField] private FieldPartyHudController partyHudController;

    private readonly List<FieldInventoryItemView> spawnedItems = new();
    private ItemData selectedItem;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
        //还需要一个清楚描述的东西
        ClearDescription();
        HideDescription();
        SetUseButtonInteractable(false);

        if (useItemButton != null)
            useItemButton.onClick.AddListener(OnUseSelectedItemClicked);
    }
    private void OnDestroy()
    {
        if (useItemButton != null)
            useItemButton.onClick.RemoveListener(OnUseSelectedItemClicked);
    }
    public override void Show()
    {
        Refresh();
        base.Show();
    }
    public override void Hide()
    {
        HideDescription();
        base.Hide();
    }
    public void Refresh()
    {
        ClearItems();
        ClearDescription();
        selectedItem = null;
        SetUseButtonInteractable(false);
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
        
        selectedItem = item;
        SetUseButtonInteractable(CanUseSelectedItem());
        ShowDescription(item);
    }
    public void OnUseSelectedItemClicked()
    {
        if (!CanUseSelectedItem())
            return;

        switch (selectedItem.itemtype)
        {
            case ItemType.Heal:
                UseHealItem(selectedItem);
                break;
        }
    }
    private void UseHealItem(ItemData item)
    {
        if (item == null)
            return;

        if (!PartyRuntimeState.TryHealFirstInjuredAliveMember(item.power, out Character healedMember))
        {
            Debug.Log("[FieldInventory] No injured alive party member found.");
            return;
        }

        if (!InventoryRuntimeState.ConsumeItem(item))
            return;

        Debug.Log($"[FieldInventory] Used {item.itemName} on {healedMember.Name}");

        partyHudController?.Refresh();
        Refresh();

        if (InventoryRuntimeState.HasItem(item))
        {
            selectedItem = item;
            ShowDescription(item);
            SetUseButtonInteractable(CanUseSelectedItem());
        }
        else
        {
            HideDescription();
        }
    }
    private bool CanUseSelectedItem()
    {
        if (selectedItem == null)
            return false;

        return selectedItem.itemtype == ItemType.Heal &&
            InventoryRuntimeState.CanUseItem(selectedItem);
    }
    private void SetUseButtonInteractable(bool interactable)
    {
        if (useItemButton != null)
            useItemButton.interactable = interactable;
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
        selectedItem = null;
        SetUseButtonInteractable(false);
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
