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
    [SerializeField] private Image Portrait;
    [SerializeField] private Image BG;
    [SerializeField] private Color _hpFillDefaultColor;
    private Tween _hpTween;

    //debug 用的代码
    // private bool _printedOnce = false;

    private void Awake()
    {
        if (hpFill != null)
        {
            _hpFillDefaultColor= hpFill.color;
        }
    }

    private BaseController _ctrl;

    public BaseController Bount=> _ctrl;

    public void Bind(BaseController ctrl)
    {
        if (_ctrl == ctrl) {
            if (_ctrl == null)
            { gameObject.SetActive(false); ; return; }      
            Refresh();
            return; }//绑定没变就别重复订阅
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
        //固定资产：头像/名字
        if(Portrait != null)
        {
            if(_ctrl.portrait != null)
            {
                Portrait.sprite = _ctrl.portrait;
            }           
        }
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
        float ratio = (_ctrl.data.MaxHp <= 0) ? 0f:(float)_ctrl.data.Hp/_ctrl.data.MaxHp;

        bool downed = _ctrl.data.isDead || _ctrl.isDead || _ctrl.data.Hp <= 0;
        bool lowHP = ratio > 0f && ratio <= 0.3f;

        gameObject.SetActive(true);
        float alpha=downed?0.5f:1f;
        //if(nameText!=null)nameText.text = _ctrl.data.Name;
        //文字 血条文字
        if(hpText!=null)hpText.text = $" {_ctrl.data.Hp}/{_ctrl.data.MaxHp}";
        //灰掉
        if (Portrait != null)
        {
            var c = Portrait.color;
            c.a = alpha;
            Portrait.color = c;
        }
        //血条Image
        if (hpFill != null)
        {         
            float t = (_ctrl.data.MaxHp <= 0) ? 0f: Mathf.Clamp01((float)_ctrl.data.Hp / _ctrl.data.MaxHp);

            //hpFill.fillAmount = t;
            _hpTween?.Kill();
            _hpTween = hpFill.DOFillAmount(t, hpTweenTime).SetEase(Ease.OutCubic);
            //Debug.Log($"[FILL SET] {hpFill.name} now={hpFill.fillAmount}");
            if (downed)
            {
                var c=hpFill.color;
                c.a = 0.5f;
                hpFill.color = c;
            }
            else if (lowHP)
            {
                hpFill.color = new Color(1f, 0.3f, 0.3f, 1f);//柔和的红色
            }
            else
            {
                hpFill.color = _hpFillDefaultColor;
            }
        }
        
    }
    private void HandleHpChanged(int prev,int cur)
    {
        //Debug.Log($"[HUD EVT] {_ctrl.data.Name} {prev}->{cur} hash={_ctrl.data.GetHashCode()}");
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
