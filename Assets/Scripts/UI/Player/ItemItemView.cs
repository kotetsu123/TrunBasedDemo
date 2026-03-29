using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class ItemItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text ItemNameTxt;
    [SerializeField] private TMP_Text ItemCountTxt;
    [SerializeField] private Button button;

    private ItemData _itemData;
    private Action<ItemData> _onClick;

    public void Bind(ItemData item, int count, Action<ItemData> onClick)
    {
        _itemData = item;
        _onClick = onClick;

        if (ItemNameTxt != null)
            ItemNameTxt.text = item != null ? item.itemName : "UnKnown";
        if (ItemCountTxt != null)
            ItemCountTxt.text = $"x{count}";
        if(button != null)
        {
            button.interactable = count > 0;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }
    private void HandleClick()
    {
        if (_itemData == null) return;
        _onClick?.Invoke(_itemData);
    }
}
