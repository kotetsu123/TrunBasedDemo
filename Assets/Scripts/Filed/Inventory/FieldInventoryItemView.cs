using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FieldInventoryItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text itemCountText;
    [SerializeField] private Image itemIconImage;

    private ItemData _item;
    private Action<ItemData> _onSelected;

    public void BindEmpty(Action<ItemData> onSelected)
    {
        _item = null;
        _onSelected = onSelected;

        if (itemCountText != null)
        {
            itemCountText.text = "";
        }
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(ItemData item,int count,Action<ItemData> onSelected)
    {
        _item = item;
        _onSelected = onSelected;
       
        if (itemCountText != null)
        {
            itemCountText.text = $"x{count}";
        }
        if (itemIconImage != null)
        {
            if (item != null&&item.icon!=null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.sprite = null;
                itemIconImage.enabled = false;
            }
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }
    private void HandleClick()
    {
        _onSelected?.Invoke(_item);
    }
}
