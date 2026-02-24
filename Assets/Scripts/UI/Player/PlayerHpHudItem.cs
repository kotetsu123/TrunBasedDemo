using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class PlayerHpHudItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpFill;//Fill Image
    [SerializeField] private float hpTweenTime = 0.25f;
    private Tween _hpTween;

    //debug 用的代码
   // private bool _printedOnce = false;

    private BaseController _ctrl;

    public BaseController Bount=> _ctrl;

    public void Bind(BaseController ctrl)
    {
        if (_ctrl == ctrl) { Refresh();return; }//绑定没变就别重复订阅
        // 解绑之前的事件
        if (_ctrl != null && _ctrl.data != null)
        {
            _ctrl.data.OnHpChanged -= HandleHpChanged;
        }
        _ctrl = ctrl;
        if (_ctrl == null || _ctrl.data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        // 绑定新的事件
        if (_ctrl!=null&&_ctrl.data!=null)
            _ctrl.data.OnHpChanged+=HandleHpChanged;
        //立即刷新一次
        Refresh();
    }
    public void Refresh()
    {
        if (_ctrl == null || _ctrl.data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        //if(nameText!=null)nameText.text = _ctrl.data.Name;
        //文字 血条文字
        if(hpText!=null)hpText.text = $" {_ctrl.data.Hp}/{_ctrl.data.MaxHp}";
        //血条Image
        if (hpFill != null)
        {
          
            float t = (_ctrl.data.MaxHp <= 0) ? 0f: Mathf.Clamp01((float)_ctrl.data.Hp / _ctrl.data.MaxHp);

            //hpFill.fillAmount = t;
            _hpTween?.Kill();
            _hpTween = hpFill.DOFillAmount(t, hpTweenTime).SetEase(Ease.OutCubic);
            //Debug.Log($"[FILL SET] {hpFill.name} now={hpFill.fillAmount}");
        }
        
    }
    private void HandleHpChanged(int prev,int cur)
    {
        Debug.Log($"[HUD EVT] {_ctrl.data.Name} {prev}->{cur} hash={_ctrl.data.GetHashCode()}");
        Refresh();
    }
    private void OnDestroy()
    {
        // 防止对象销毁时还挂着订阅
        if (_ctrl != null && _ctrl.data != null)
        {
            _ctrl.data.OnHpChanged -= HandleHpChanged;
        }
    }
}
