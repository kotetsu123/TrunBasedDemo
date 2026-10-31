using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleHitFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector3 localPunch = new Vector3(0f, 0f, -0.16f);
    [SerializeField, Min(0.01f)] private float duration = 0.25f;
    [SerializeField, Range(1, 10)] private int vibrato = 4;
    [SerializeField, Range(0f, 1f)] private float elasticity = 0.6f;

    private Tween _tween;
    private Vector3 _restPosition;
    private bool _bound;

    private void OnEnable()
    {
        if (controller == null) controller = GetComponent<BaseController>();
        if (controller == null || visualRoot == null) return;
        if (visualRoot == controller.transform || !visualRoot.IsChildOf(controller.transform)
            || visualRoot.GetComponentInChildren<Collider>(true) != null)
        {
            Debug.LogWarning("[BattleHitFeedback] Visual Root must be a collider-free child of the character.", this);
            return;
        }

        _restPosition = visualRoot.localPosition;
        controller.OnDamageTaken += HandleDamageTaken;
        _bound = true;
    }

    private void HandleDamageTaken(int damage)
    {
        if (damage <= 0 || !isActiveAndEnabled || visualRoot == null) return;

        StopAndRestore();
        _tween = visualRoot.DOPunchPosition(localPunch, Mathf.Max(0.01f, duration),
                Mathf.Max(1, vibrato), Mathf.Clamp01(elasticity))
            .SetLink(visualRoot.gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                if (visualRoot != null) visualRoot.localPosition = _restPosition;
                _tween = null;
            });
    }

    private void OnDisable()
    {
        if (_bound && controller != null)
            controller.OnDamageTaken -= HandleDamageTaken;
        StopAndRestore();
        _bound = false;
    }

    private void OnDestroy()
    {
        StopAndRestore();
    }

    private void StopAndRestore()
    {
        _tween?.Kill();
        _tween = null;
        if (_bound && visualRoot != null)
            visualRoot.localPosition = _restPosition;
    }
}
