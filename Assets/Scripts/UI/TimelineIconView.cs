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

    private Tween _tween;
    private bool _active;

    private void Awake()
    {
        ForceInactive();
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
    public void ForceInactive()
    {
        _tween?.Kill();
        _tween = null;

        _active = false;
        if (visualRoot != null)
            visualRoot.localScale = Vector3.one;

        if (glow != null)
            glow.alpha = 0f;

    }
}
