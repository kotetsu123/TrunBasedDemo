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

    // 记录这个 UI 格子对应 InventoryRuntimeState.Slots 里的第几个 slot。
    // 之后拖拽时会用这个 index 去交换两个 slot 的运行时数据。
    private int _slotIndex = -1;


    public int SlotIndex => _slotIndex; 

    public void BindEmpty(int slotIndex,Action<ItemData> onSelected)
    {
        _slotIndex = slotIndex;
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

    public void Bind(ItemData item,int count,int slotIndex,Action<ItemData> onSelected)
    {
        _item = item;
        _slotIndex = slotIndex;
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
