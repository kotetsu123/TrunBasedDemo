using DG.Tweening;
using TMPro;
using UnityEngine;

public class FieldInteractionPromptController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private float fadeDuration = 0.12f;

    private Tween fadeTween;
    private Object currentOwner;

    public static FieldInteractionPromptController Current { get; private set; }

    private void Awake()
    {
        Current = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        fadeTween?.Kill();
    }

    public void Show(Object owner, string message)
    {
        if (canvasGroup == null)
            return;

        currentOwner = owner;

        if (promptText != null)
            promptText.text = message;

        fadeTween?.Kill();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        fadeTween = canvasGroup.DOFade(1f, fadeDuration).SetLink(gameObject);
    }

    public void Hide(Object owner)
    {
        if (canvasGroup == null)
            return;

        if (currentOwner != null && currentOwner != owner)
            return;

        currentOwner = null;
        fadeTween?.Kill();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        fadeTween = canvasGroup.DOFade(0f, fadeDuration).SetLink(gameObject);
    }

    public void HideImmediate()
    {
        if (canvasGroup == null)
            return;

        fadeTween?.Kill();
        currentOwner = null;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
