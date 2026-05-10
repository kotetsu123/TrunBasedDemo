using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FiledPartyHudItem : MonoBehaviour
{
    [SerializeField] private Image hpfill;
    [SerializeField] private Color _hpFillDefaultColor;
    // [SerializeField]

    private Character _boundData;

    private void Start()
    {
        if (hpfill != null)
        {
            _hpFillDefaultColor = hpfill.color;
        }
    }


    public void Bind(Character data)
    {
        Unbind();

        _boundData = data;

        if (_boundData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _boundData.OnHpChanged +=HandleHpChanged ;

        Refresh();
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
            hpfill.color = _hpFillDefaultColor;//恢复默认颜色
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
    }
    private void OnDestroy()
    {
        Unbind();
    }
}

