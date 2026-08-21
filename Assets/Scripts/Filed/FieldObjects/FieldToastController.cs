using DG.Tweening;
using TMPro;
using UnityEngine;

public class FieldToastController : BasePanel
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float visibleSeconds = 1.8f;

    private Tween hideDelayTween;

    public static FieldToastController Current { get; private set; }

    protected override void Awake()
    {
        useInteraction = false;
        base.Awake();
        Current = this;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        hideDelayTween?.Kill();
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (messageText != null)
            messageText.text = message;

        Show();

        hideDelayTween?.Kill();
        hideDelayTween = DOVirtual.DelayedCall(visibleSeconds, Hide)
            .SetLink(gameObject);
    }
}
