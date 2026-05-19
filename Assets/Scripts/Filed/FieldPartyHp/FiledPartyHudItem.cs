using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FiledPartyHudItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image hpfill;
    [SerializeField] private Color _hpFillDefaultColor;
    [SerializeField] private Image portraitImage;
    // [SerializeField]

    private Character _boundData;
    private Action<Character> _onClicked;
    private Graphic[] _raycastGraphics;

    private void Awake()
    {
        _raycastGraphics = GetComponentsInChildren<Graphic>(true);
        SetRaycastTarget(false);

        if (hpfill != null)
        {
            _hpFillDefaultColor = hpfill.color;
             _hpFillDefaultColor.a = 1f; //确保默认颜色的alpha为1
        }
    }
    private void Start()
    {

    }


    public void Bind(Character data, Action<Character> onClicked = null)
    {
        Unbind();

        _boundData = data;
        _onClicked = onClicked;
        SetRaycastTarget(_onClicked != null);

        if (_boundData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _boundData.OnHpChanged +=HandleHpChanged ;

        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_boundData == null)
            return;

        _onClicked?.Invoke(_boundData);
    }
    private void SetRaycastTarget(bool enabled)
    {
        if (_raycastGraphics == null)
            return;

        foreach (var graphic in _raycastGraphics)
        {
            if (graphic != null)
                graphic.raycastTarget = enabled;
        }
    }
    private void Refresh()
    {
        
        if (_boundData == null)
            return;
        if(hpfill!=null)
            hpfill.fillAmount=_boundData.MaxHp<=0?0f:(float)_boundData.Hp/_boundData.MaxHp;
        bool lowHP = hpfill.fillAmount > 0f && hpfill.fillAmount <= 0.3f;
        if(lowHP)
            hpfill.color = new Color(1f, 0.3f, 0.3f, 1f);//修改颜色为红色
        else
        {
            Color c = _hpFillDefaultColor;
            c.a = 1f; //确保颜色的alpha为1
            hpfill.color = c;//恢复默认颜色
        }
            
        if (portraitImage != null)
        {
            portraitImage.sprite = _boundData.Portrait;
            portraitImage.enabled=_boundData.Portrait != null;
        }
    }
    private void HandleHpChanged(int prev,int cur)
    {
        Refresh();
    }
    
    private void Unbind()
    {
        if (_boundData == null)
            return ;

        _boundData.OnHpChanged -= HandleHpChanged;
        _boundData = null;
        _onClicked = null;
        SetRaycastTarget(false);
    }
    private void OnDestroy()
    {
        Unbind();
    }
}

