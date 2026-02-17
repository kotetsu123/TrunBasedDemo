using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
public class TimelineIconView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private CanvasGroup glow;

    [Header("Anim")]
    [SerializeField] private float actionScale = 1.12f;
    [SerializeField] private float animTime = 0.25f;

    [Header("Glow")]
    [SerializeField] private float nextAlpha = 0.35f;
    [SerializeField] private float activeAlpha = 0.65f;

    [SerializeField] private Image glowImage;
    private Color _defalutGlowColor;

    public enum TimeLineState {Normal,Next,Active }


    private Tween _tween;
    private bool _active;
    private TimeLineState _state= TimeLineState.Normal;

    private void Awake()
    {
        ForceInactive(TimeLineState.Normal);
            if (glowImage != null)
                _defalutGlowColor = glowImage.color;
    }
    private void Reset()
    {
        visualRoot = GetComponent<RectTransform>();
    }
    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;
        _active = false;

        //保底：避免对象disable后保持放大
        if(visualRoot)visualRoot.localScale = Vector3.one;
        if(glow)glow.alpha = 0;
    }
    public void SetActive(bool active)
    {
               if (_active == active) return;
        _active = active;

        _tween?.Kill();

        //如果对象已被销毁/禁用，直接退出
        if (!isActiveAndEnabled || visualRoot == null) return;
        var seq = DOTween.Sequence()
            .SetLink(gameObject)//绑定生命周期 物体销毁时自动kill
            .SetUpdate(true); //忽略TimeScale

        if (active)
        {
            //放大+发光
            seq.Append(visualRoot.DOScale(actionScale, animTime).SetEase(Ease.OutBack));
            if (glow) seq.Join(glow.DOFade(1f, animTime));
        }
        else
        {
            //缩回+熄灭
            seq.Append(visualRoot.DOScale(1, animTime).SetEase(Ease.OutBack));
              if(glow)  seq.Join(glow.DOFade(0, animTime));
        }
        _tween = seq;
    }
    public void ForceInactive(TimeLineState state)
    {
        _tween?.Kill();
        _tween = null;
        _state = state;

        _active = false;
        if (visualRoot)
            visualRoot.localScale = Vector3.one;

        if (glow)
            glow.alpha=(state==TimeLineState.Next)?nextAlpha:0f;
    }
    public void SetState(TimeLineState state,Color nextGlowColor)
    {
        Debug.Log($"[SetState] f={Time.frameCount} t={Time.time:F3} id={GetInstanceID()} state={state}");

        //Debug.Log($"[iconView]{name}#{GetInstanceID()}=>{state} glowAlpha={(glow?glow.alpha:-1f)}");
        //if (_state == state) return;
        if (_state == state)
        {
            Debug.Log($"[SetState] SKIP same state id={GetInstanceID()} state={state}");
            return;
        }
    
       _tween ?.Kill();
        _state = state;
        if (!isActiveAndEnabled || visualRoot == null) return;

       
        
        //=====目标值=====
        float targetScale=(state==TimeLineState.Active)? actionScale:1f;

        float tarrgetAlpha =
            (state == TimeLineState.Active) ? activeAlpha :
            (state == TimeLineState.Next) ? nextAlpha :
            0f;
        //颜色：next状态可能需要特殊颜色，active和normal使用默认颜色
        if (glowImage != null)
        {
            glowImage.color= (state == TimeLineState.Next) ? nextGlowColor :  _defalutGlowColor;
        }

        //Next的颜色
        if (state == TimeLineState.Next && glowImage!=null)
            glowImage.color= nextGlowColor;

        var seq = DOTween.Sequence().SetLink(gameObject);//.SetUpdate(true);
        Debug.Log($"[SetState] f={Time.frameCount} created tween id={GetInstanceID()} state={state} targetScale={targetScale}");


        //scaled 动画

        seq.Append(visualRoot.DOScale(targetScale, animTime).SetEase(state == TimeLineState.Active ? Ease.OutBack : Ease.OutQuint));

        seq.OnUpdate(() =>
        {
            if (state == TimeLineState.Active)
                Debug.Log($"[TweenUpdate] id={GetInstanceID()} scale  = {visualRoot.localScale}");
        });
        seq.OnComplete(() => 
        { 
            Debug.Log($"[TweenDone] id={GetInstanceID()} finalScale={visualRoot.localScale}"); 
        });

        //glow 淡入淡出
        if(glow!=null)
            seq.Join(glow.DOFade(tarrgetAlpha, animTime));


        _tween = seq;
        Debug.Log($"[ScaleTest] visualRoot={visualRoot.name} parent={visualRoot.parent?.name} startScale={visualRoot.localScale}");
        Debug.Log($"[ScaleParam] actionScale={actionScale} animTime={animTime}");


    }

}
