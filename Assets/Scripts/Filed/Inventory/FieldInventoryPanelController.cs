using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FieldInventoryPanelController : BasePanel
{
    [Header("Item List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private FieldInventoryItemView itemViewPrefab;

    [Header("Description")]
    [SerializeField] private TMP_Text selectedItemNameText;
    [SerializeField] private TMP_Text selectedItemDescriptionText;

    private readonly List<FieldInventoryItemView> spawnedItems = new();

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
        //还需要一个清楚描述的东西
        ClearDescription();
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
        foreach(var pair in InventoryRuntimeState.Items)
        {
            ItemData item = pair.Key;
            int count = pair.Value;

            if (item == null || count <= 0)
                continue;

            FieldInventoryItemView view = Instantiate(itemViewPrefab, contentRoot);
            view.Bind(item, count, HandleSelected);
            spawnedItems.Add(view);
        }
    }
    private void HandleSelected(ItemData item)
    {
        if (item == null)
        {
            ClearDescription();
            return;
        }
        if (selectedItemNameText != null)
            selectedItemNameText.text = item.name;
        if (selectedItemDescriptionText != null)
            selectedItemDescriptionText.text = item.description;
       
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
