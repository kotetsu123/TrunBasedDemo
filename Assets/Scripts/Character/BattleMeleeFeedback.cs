using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleMeleeFeedback : MonoBehaviour
{
    public enum MeleePhase { Idle, Approach, Strike, Return }

    [SerializeField] private BaseController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform motionRoot;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField, Min(0f)] private float blinkTime = 0.06f;
    [SerializeField, Min(0f)] private float contactGap = 0.15f;
    [SerializeField, Range(0f, 1f)] private float impactNormalizedTime = 0.6f;
    [SerializeField, Min(0f)] private float blendTime = 0.06f;

    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private Renderer[] _hiddenRenderers;
    private bool[] _rendererEnabled;
    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private bool _playing;

    public MeleePhase Phase { get; private set; }
    public bool IsPlaying => _playing;
    public Transform MotionRoot => motionRoot;
    public bool IsReady => isActiveAndEnabled && controller != null && animator != null
        && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
        && attackClip != null && motionRoot != null && motionRoot != controller.transform
        && motionRoot.IsChildOf(controller.transform)
        && motionRoot.GetComponentInChildren<Collider>(true) == null;

    public Vector3 GetContactPosition(Transform target)
    {
        Vector3 home = motionRoot.position;
        Vector3 direction = target.position - home;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return home;
        direction.Normalize();
        var targetCollider = target.GetComponent<Collider>();
        Vector3 surface = targetCollider != null && targetCollider.enabled
            ? targetCollider.ClosestPoint(new Vector3(home.x, targetCollider.bounds.center.y, home.z))
            : target.position - direction * 0.5f;
        var body = controller.GetComponent<Collider>();
        float radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) : 0.4f;
        Vector3 contact = surface - direction * (radius + Mathf.Max(0f, contactGap));
        contact.y = home.y;
        // Already-close targets should not make the actor move backwards before attacking.
        return Vector3.Dot(contact - home, direction) > 0f ? contact : home;
    }

    public IEnumerator Play(Transform target, Action onImpact, Action onApproachComplete = null, Action onReturnStart = null)
    {
        if (_playing || !CanAnimate() || !CanStrike(target)) yield break;
        _restPosition = motionRoot.localPosition;
        _restRotation = motionRoot.localRotation;
        _playing = true;
        try
        {
            Vector3 contact = GetContactPosition(target);
            Face(target.position - motionRoot.position);
            Phase = MeleePhase.Approach;
            yield return Blink(contact, target);
            if (!CanContinue()) yield break;

            if (CanStrike(target))
            {
                Face(target.position - motionRoot.position);
                Phase = MeleePhase.Strike;
                onApproachComplete?.Invoke();
                animator.CrossFadeInFixedTime(Attack, blendTime, 0, 0f);
                float duration = Mathf.Max(0.01f, attackClip.length);
                float hitTime = duration * Mathf.Clamp01(impactNormalizedTime);
                yield return Wait(hitTime);
                if (!CanContinue()) yield break;
                if (CanStrike(target)) onImpact?.Invoke();
                yield return Wait(duration - hitTime);
                if (!CanContinue()) yield break;
            }

            // Formation and click colliders remain at home; only the display hierarchy travels.
            Phase = MeleePhase.Return;
            onReturnStart?.Invoke();
            Vector3 home = motionRoot.parent.TransformPoint(_restPosition);
            yield return Blink(home, null);
        }
        finally { Restore(); }
    }

    private IEnumerator Blink(Vector3 destination, Transform requiredTarget)
    {
        if (Vector3.Distance(motionRoot.position, destination) < 0.02f) yield break;
        HideRenderers();
        yield return Wait(Mathf.Max(0f, blinkTime));
        if (!CanContinue()) yield break;
        if (Phase == MeleePhase.Approach)
        {
            if (!CanStrike(requiredTarget)) yield break;
            destination = GetContactPosition(requiredTarget);
        }
        motionRoot.position = destination;
        RestoreRenderers();
    }

    private void HideRenderers()
    {
        if (_hiddenRenderers != null) return;
        _hiddenRenderers = motionRoot.GetComponentsInChildren<Renderer>(true);
        _rendererEnabled = new bool[_hiddenRenderers.Length];
        for (int i = 0; i < _hiddenRenderers.Length; i++)
        {
            _rendererEnabled[i] = _hiddenRenderers[i].enabled;
            _hiddenRenderers[i].enabled = false;
        }
    }

    private void RestoreRenderers()
    {
        if (_hiddenRenderers == null) return;
        for (int i = 0; i < _hiddenRenderers.Length; i++)
            if (_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = _rendererEnabled[i];
        _hiddenRenderers = null;
        _rendererEnabled = null;
    }

    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (!CanContinue()) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private static bool CanStrike(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var unit = target.GetComponent<BaseController>();
        return unit != null && unit.data != null && !unit.isDead && unit.data.Hp > 0;
    }

    private bool CanAnimate() => this != null && IsReady && motionRoot.gameObject.activeInHierarchy
        && controller.data != null && !controller.isDead && controller.data.Hp > 0;
    private bool CanContinue() => _playing && CanAnimate();

    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f) motionRoot.rotation = Quaternion.LookRotation(direction);
    }

    public void Cancel() => Restore();

    private void Restore()
    {
        RestoreRenderers();
        if (!_playing) return;
        if (motionRoot != null)
        {
            motionRoot.localPosition = _restPosition;
            motionRoot.localRotation = _restRotation;
        }
        _playing = false;
        Phase = MeleePhase.Idle;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.Play(Idle, 0, 0f);
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
