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
    [SerializeField] private float animTime = 0.12f;

    [Header("Glow")]
    [SerializeField] private float nextAlpha = 0.35f;
    [SerializeField] private float activeAlpha = 0.65f;

    [SerializeField] private Image glowImage;

    public enum TimeLineState {Normal,Next,Active }


    private Tween _tween;
    private bool _active;
    private TimeLineState _state= TimeLineState.Normal;

    private void Awake()
    {
        ForceInactive(TimeLineState.Normal);
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
        if (_state == state) return;
        _state = state;

        _tween?.Kill();
        if (!isActiveAndEnabled || visualRoot == null) return;

        //颜色只对next状态有意义（active 保留原来黄色高光）
        
        if(state==TimeLineState.Next||glowImage!=null)
        {
            glowImage.color = nextGlowColor;
        }
        float targetScale=(state==TimeLineState.Active)? actionScale:1f;
        float tarrgetAlpha =
            (state == TimeLineState.Active) ? activeAlpha :
            (state == TimeLineState.Next) ? nextAlpha :
            0f;

        var seq= DOTween.Sequence().SetLink(gameObject).SetUpdate(true);
        seq.Append(visualRoot.DOScale(targetScale, animTime).SetEase(state == TimeLineState.Active ? Ease.OutBack : Ease.OutQuint));
        if(glow)seq.Join(glow.DOFade(tarrgetAlpha, animTime));


        _tween = seq;
    }

}
