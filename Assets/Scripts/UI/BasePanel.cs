using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BasePanel : MonoBehaviour
{
    [SerializeField]protected CanvasGroup canvasGroup;
    [SerializeField] protected float fadeDuration = 0.15f;

    protected Tween currentTween;

    public virtual bool IsOpen=>canvasGroup!=null&&canvasGroup.alpha > 0.99f;

    protected virtual void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }   

    public virtual void Show()
    {
        if (canvasGroup == null) return;

        currentTween?.Kill();

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        currentTween= canvasGroup.DOFade(1f, fadeDuration);
    }
    public virtual void Hide()
    {
        if (canvasGroup == null) return;
        currentTween?.Kill();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        currentTween= canvasGroup.DOFade(0f, fadeDuration);
    }
    public virtual void HideImmediate()
    {
        if (canvasGroup == null) return;

        currentTween?.Kill();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;
    }


}
