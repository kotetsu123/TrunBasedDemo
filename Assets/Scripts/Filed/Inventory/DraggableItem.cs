using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private FieldInventoryItemView _sourceView;
    private Graphic[] _graphics;


    public int SourceSlotIndex=>_sourceView != null ? _sourceView.SlotIndex : -1;
    public Image image;

    public Transform parentAfterDrag;

    private void Awake()
    {
        //因为draggableitem 挂载在FieldInventoryItemView的子物体上，所以可以通过GetComponentInParent来获取到FieldInventoryItemView组件
        _sourceView = GetComponentInParent<FieldInventoryItemView>();
        _graphics = GetComponentsInChildren<Graphic>();
    }
    private void SetRaycastTarget(bool value)
    {
        foreach(var graphic in _graphics)
        {
            if(graphic!=null)
                graphic.raycastTarget=value;
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("begin drag");
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        // 拖拽时关闭 icon/count 的 raycast，让下面的 InventorySlot 可以收到 OnDrop。
        SetRaycastTarget(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log(" dragging");
        transform.position=Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(" drag end");
        SetRaycastTarget(true);
        transform.SetParent(parentAfterDrag);
        transform.localPosition=Vector3.zero;
    }
}
