using System;
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
    [SerializeField] private FieldInventoryPartyTargetPanelController partyTargetPanel;

    [Header("Feedback")]
    [SerializeField] private FieldToastController toastController;

    private readonly List<FieldInventoryItemView> spawnedItems = new();
    private ItemData selectedItem;
    private bool isSelectingTarget;

    public bool IsSelectingTarget => isSelectingTarget;

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

        if (partyTargetPanel != null)
            partyTargetPanel.OnPartyMemberSelected += HandlePartyMemberSelected;
    }
    private void OnDestroy()
    {
        if (useItemButton != null)
            useItemButton.onClick.RemoveListener(OnUseSelectedItemClicked);

        if (partyTargetPanel != null)
            partyTargetPanel.OnPartyMemberSelected -= HandlePartyMemberSelected;
    }
    public override void Show()
    {
        Refresh();
        base.Show();
    }
    public override void Hide()
    {
        HideDescription();
        partyTargetPanel?.Hide();
        base.Hide();
    }
    private void Update()
    {
        if (!isSelectingTarget)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelTargetSelection();
        }
    }
    public void Refresh()
    {
        ClearItems();
        ClearDescription();
        selectedItem = null;
        isSelectingTarget = false;
        partyTargetPanel?.Hide();
        SetUseButtonInteractable(false);
        if (contentRoot == null || itemViewPrefab == null)
            return;

        // Field 背包按运行时 slot 顺序生成 UI。这样 UI 第 N 格就对应数据里的第 N 格。
        IReadOnlyList<InventorySlotState> slots = InventoryRuntimeState.Slots;
        int requiredSlotCount = Mathf.Max(slotCount, slots.Count);

        for (int i = 0; i < requiredSlotCount; i++)
        {
            FieldInventoryItemView view = Instantiate(itemViewPrefab, contentRoot);

            if (i < slots.Count && slots[i] != null && !slots[i].IsEmpty)
            {
                InventorySlotState slot = slots[i];
                view.Bind(slot.item, slot.count,i, HandleSelected);
            }
            else
            {
                view.BindEmpty(i,HandleSelected);
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
        
        isSelectingTarget = false;
        partyTargetPanel?.Hide();
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
            case ItemType.RestoreMp:
            case ItemType.Revive:
                StartTargetSelection();
                break;
            case ItemType.Buff:
                Debug.Log("[FieldInventory] Buff item is reserved, but buff effect is not implemented yet.");
                break;
        }
    }
    private void StartTargetSelection()
    {
        isSelectingTarget = true;
        SetUseButtonInteractable(false);
        partyTargetPanel?.Show();
        Debug.Log("[FieldInventory] Select a party member to use item.");
    }
    private void CancelTargetSelection()
    {
        isSelectingTarget = false;
        partyTargetPanel?.Hide();
        SetUseButtonInteractable(CanUseSelectedItem());

        Debug.Log("[FieldInventory] Item target selection canceled.");
    }
    private void HandlePartyMemberSelected(Character target)
    {
        if (!isSelectingTarget)
            return;
        if (selectedItem == null || !InventoryRuntimeState.CanUseItem(selectedItem))
            return;

        UseSelectedItemOnTarget(selectedItem, target);
    }
    private void UseSelectedItemOnTarget(ItemData item, Character target)
    {
        if (item == null || target == null)
            return;

        if (!TryApplyFieldItemEffect(item, target))
        {
            string message = GetItemUseFialMeassage(item, target);
            SHowToast(message);

            Debug.Log("[FieldInventory] Selected party member cannot use this item.");
            return;
        }

        if (!InventoryRuntimeState.ConsumeItem(item))
            return;

        Debug.Log($"[FieldInventory] Used {item.itemName} on {target.Name}");

        isSelectingTarget = false;
        partyTargetPanel?.Hide();
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

 
    private bool TryApplyFieldItemEffect(ItemData item, Character target)
    {
        // Field item effects operate directly on PartyRuntimeState because there is no battle controller here.
        switch (item.itemtype)
        {
            case ItemType.Heal:
                return PartyRuntimeState.TryHealMember(target, item.power);
            case ItemType.RestoreMp:
                return PartyRuntimeState.TryRestoreMpMember(target, item.power);
            case ItemType.Revive:
                return PartyRuntimeState.TryReviveMember(target, item.power);
            case ItemType.Buff:
                Debug.Log("[FieldInventory] Buff item type is only a placeholder for now.");
                return false;
            default:
                return false;
        }
    }
  

    private string GetItemUseFialMeassage(ItemData item, Character target)
    {
        if (item == null)
            return "No item selected.";
        if (target == null)
            return "No target selected.";
        switch (item.itemtype)
        {
            case ItemType.Heal:
                if (target.isDead || target.Hp <= 0)
                    return $"{target.Name} cannot be healed while down";
                if (target.Hp >= target.MaxHp)
                    return $"{target.Name} is already at full HP.";
                return $"{target.Name} cannot use this item.";
            case ItemType.RestoreMp:
                if (target.isDead || target.Hp <= 0)
                    return $"{target.Name} cannot restore MP while down";
                if (target.Mp >= target.MaxMp)
                    return $"{target.Name} is already at full MP.";
                return $"{target.Name} cannot use this item.";
            case ItemType.Revive:
                if (!target.isDead && target.Hp > 0)
                    return $"{target.Name} does not need revival";
                return $"{target.Name} cannot be revived.";
            default:
                return $"Cannot use {item.itemName} on {target.Name}.";
        }
    }
    private void SHowToast(string message)
    {
        if(toastController==null)
            toastController=FieldToastController.Current;
        toastController?.ShowMessage(message);
    }
    private bool CanUseSelectedItem()
    {
        if (selectedItem == null)
            return false;

        return !isSelectingTarget &&
            IsFieldUsableItem(selectedItem) &&
            InventoryRuntimeState.CanUseItem(selectedItem);
    }
    private bool IsFieldUsableItem(ItemData item)
    {
        if (item == null)
            return false;

        return item.itemtype == ItemType.Heal ||
            item.itemtype == ItemType.RestoreMp ||
            item.itemtype == ItemType.Revive;
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
        isSelectingTarget = false;
        partyTargetPanel?.Hide();
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
