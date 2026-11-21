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
    [SerializeField] private BattleBlinkFeedback blinkFeedback;
    [SerializeField] private SkillData backstabSkill;
    [SerializeField] private BattleBackstabSlashFeedback backstabSlash;
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
    public bool MatchesBackstab(SkillData skill) => skill != null && skill == backstabSkill;
    public bool IsReady => isActiveAndEnabled && controller != null && animator != null
        && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null
        && attackClip != null && motionRoot != null && motionRoot != controller.transform
        && motionRoot.IsChildOf(controller.transform)
        && motionRoot.GetComponentInChildren<Collider>(true) == null;

    public Vector3 GetContactPosition(Transform target, bool fromBehind = false)
    {
        Vector3 home = motionRoot.position;
        Vector3 direction = target.position - home;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return home;
        direction.Normalize();
        var targetCollider = target.GetComponent<Collider>();
        Vector3 outward = fromBehind ? direction : -direction;
        Vector3 surface = target.position + outward * 0.5f;
        if (targetCollider != null && targetCollider.enabled)
        {
            Bounds bounds = targetCollider.bounds;
            // The current enemy cubes have no authored facing; use the far side from the caster.
            Vector3 probe = fromBehind
                ? bounds.center + outward * (bounds.extents.magnitude + 1f)
                : new Vector3(home.x, bounds.center.y, home.z);
            surface = targetCollider.ClosestPoint(probe);
        }
        var body = controller.GetComponent<Collider>();
        float radius = body != null ? Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) : 0.4f;
        Vector3 contact = surface + outward * (radius + Mathf.Max(0f, contactGap));
        contact.y = home.y;
        // Already-close targets should not make the actor move backwards before attacking.
        return fromBehind || Vector3.Dot(contact - home, direction) > 0f ? contact : home;
    }

    public IEnumerator Play(Transform target, Action onImpact, Action onApproachComplete = null,
        Action onReturnStart = null, bool fromBehind = false)
    {
        if (_playing || !CanAnimate() || !CanStrike(target)) yield break;
        _restPosition = motionRoot.localPosition;
        _restRotation = motionRoot.localRotation;
        _playing = true;
        try
        {
            Vector3 contact = GetContactPosition(target, fromBehind);
            Face(target.position - motionRoot.position);
            Phase = MeleePhase.Approach;
            yield return Blink(contact, target, fromBehind);
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
                if (CanStrike(target)) ResolveImpact(target, onImpact, fromBehind);
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

    private void ResolveImpact(Transform target, Action onImpact, bool fromBehind)
    {
        if (!fromBehind || backstabSlash == null)
        {
            onImpact?.Invoke();
            return;
        }

        var targetData = target.GetComponent<BaseController>().data;
        int previousHp = targetData.Hp;
        var collider = target.GetComponent<Collider>();
        Bounds bounds = collider != null && collider.enabled ? collider.bounds : new Bounds(target.position, Vector3.one);
        onImpact?.Invoke();
        if (targetData.Hp < previousHp && backstabSlash != null)
            backstabSlash.Emit(bounds);
    }

    private IEnumerator Blink(Vector3 destination, Transform requiredTarget, bool fromBehind = false)
    {
        if (Vector3.Distance(motionRoot.position, destination) < 0.02f) yield break;
        if (blinkFeedback != null) blinkFeedback.Emit(motionRoot, false);
        HideRenderers();
        yield return Wait(Mathf.Max(0f, blinkTime));
        if (!CanContinue()) yield break;
        if (Phase == MeleePhase.Approach)
        {
            if (!CanStrike(requiredTarget)) yield break;
            destination = GetContactPosition(requiredTarget, fromBehind);
        }
        motionRoot.position = destination;
        if (Phase == MeleePhase.Return) motionRoot.localRotation = _restRotation;
        else Face(requiredTarget.position - motionRoot.position);
        RestoreRenderers();
        if (blinkFeedback != null) blinkFeedback.Emit(motionRoot, true);
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

    public void Cancel()
    {
        if (blinkFeedback != null) blinkFeedback.Clear();
        Restore();
    }

    private void Restore()
    {
        if (!CanAnimate() && blinkFeedback != null) blinkFeedback.Clear();
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
