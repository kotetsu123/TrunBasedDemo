using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


public class PlayerHudItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpFill;//Fill Image

    [SerializeField] private float hpTweenTime = 0.25f;
    [SerializeField] private float mpTweenTime = 0.25f;

    [SerializeField] private TMP_Text mpText;
    [SerializeField] private Image mpFill;
    
    [SerializeField] private Image Portrait;
    [SerializeField] private Image BG;
    [SerializeField] private Color _hpFillDefaultColor;
    private Tween _hpTween;
    private Tween _mpTween;

    private Character _boundData;

   
    // private bool _printedOnce = false;

    private void Awake()
    {
        if (hpFill != null)
        {
            _hpFillDefaultColor= hpFill.color;
        }
    }

    private BaseController _ctrl;
    //private Character _boundData;

    public BaseController Bount=> _ctrl;

    public void Bind(BaseController ctrl)
    {
        if (_ctrl == ctrl && _ctrl != null && _ctrl.data != null)
        {
            gameObject.SetActive(false);
            Refresh();
            return;
        }

        //绑定新角色前，先解绑旧Character的事件
        Unbind();

        _ctrl = ctrl;

        //没有角色/没有数据时，隐藏这个HudItem
        if (_ctrl == null || _ctrl.data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        //记录当前真正订阅的Character 数据
        _boundData=_ctrl.data;
        _boundData.OnHpChanged += HandleHpChanged;
        _boundData.OnMpChanged += HandleMpChanged;
        //绑定头像//统一从character数据里取头像，避免ctrl切换时忘了更新头像
        if (Portrait != null && _ctrl.portrait != null)
        {
            Portrait.sprite= _boundData.Portrait;
            Portrait.enabled=_boundData.Portrait != null;
        }
        //刷新
        Refresh(); 
      
    }
    public void Refresh()
    {
        if (_ctrl == null || _ctrl.data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        float hpRatio = (_ctrl.data.MaxHp <= 0) ? 0f:(float)_ctrl.data.Hp/_ctrl.data.MaxHp;
        float mpRatio = (_ctrl.data.MaxMp <= 0) ? 0f : (float)_ctrl.data.Mp / _ctrl.data.MaxMp;

        bool downed = _ctrl.data.isDead || _ctrl.isDead || _ctrl.data.Hp <= 0;
        bool lowHP = hpRatio > 0f && hpRatio <= 0.3f;

        gameObject.SetActive(true);
        float alpha=downed?0.5f:1f;
        //if(nameText!=null)nameText.text = _ctrl.data.Name;
        //文字显示
        if(hpText!=null)hpText.text = $" {_ctrl.data.Hp}/{_ctrl.data.MaxHp}";
        //头像
        if (Portrait != null)
        {
            var c = Portrait.color;
            c.a = alpha;
            Portrait.color = c;
        }
        //血条Image
        if (hpFill != null)
        {         
            _hpTween?.Kill();
            _hpTween = hpFill.DOFillAmount(hpRatio, hpTweenTime).SetEase(Ease.OutCubic);
            //Debug.Log($"[FILL SET] {hpFill.name} now={hpFill.fillAmount}");
            if (downed)
            {
                var c=hpFill.color;
                c.a = 0.5f;
                hpFill.color = c;
            }
            else if (lowHP)
            {
                hpFill.color = new Color(1f, 0.3f, 0.3f, 1f);//修改颜色为红色
            }
            else
            {
                hpFill.color = _hpFillDefaultColor;
            }
        }
        //mp文字显示
        if (mpText != null) mpText.text = $"{_ctrl.data.Mp}/ {_ctrl.data.MaxMp}";
        //mpimage
        if (mpFill != null)
        {
            _mpTween?.Kill();
            _mpTween=mpFill.DOFillAmount(mpRatio,mpTweenTime).SetEase(Ease.OutCubic);
            
            var c = mpFill.color;
            c.a = alpha;
            mpFill.color = c;
           
        }
        
        
    }
    private void HandleHpChanged(int prev,int cur)
    {
        //Debug.Log($"[HUD EVT] {_ctrl.data.Name} {prev}->{cur} hash={_ctrl.data.GetHashCode()}");
        Refresh();
    }
    private void HandleMpChanged(int prev,int cur)
    {
        Refresh();
    }
    private void OnDestroy()
    {
        
        if (_ctrl != null && _ctrl.data != null)
        {
            _ctrl.data.OnHpChanged -= HandleHpChanged;
            _ctrl.data.OnMpChanged -= HandleMpChanged;  
        }
        Unbind();
        _hpTween?.Kill();
        _mpTween?.Kill();
    }

    private void Unbind()
    {
        if (_boundData == null)
            return;
        _boundData.OnHpChanged -= HandleHpChanged;
        _boundData.OnMpChanged -= HandleMpChanged;

        _boundData = null;
    }
}
