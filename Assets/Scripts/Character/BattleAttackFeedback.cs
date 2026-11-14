using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleAttackFeedback : MonoBehaviour
{
    [SerializeField] private BaseController controller;
    [SerializeField] private Transform motionRoot;
    [SerializeField] private SkillData attackSkill;
    [SerializeField] private BattleMeleeFeedback meleeFeedback;
    [SerializeField, Min(0f)] private float lungeDistance = 0.45f;
    [SerializeField, Min(0.01f)] private float forwardTime = 0.14f;
    [SerializeField, Min(0f)] private float impactHoldTime = 0.06f;
    [SerializeField, Min(0.01f)] private float returnTime = 0.2f;

    private Sequence _sequence;
    private Vector3 _restPosition;
    private bool _playing;

    public bool MatchesSkill(SkillData skill) => skill != null && skill == attackSkill;
    public bool UsesMeleeAnimation => isActiveAndEnabled && meleeFeedback != null && meleeFeedback.IsReady;

    public IEnumerator Play(Transform target, Action onImpact, Action onApproachComplete = null, Action onReturnStart = null)
    {
        if (_playing) yield break;
        if (controller == null) controller = GetComponent<BaseController>();
        if (UsesMeleeAnimation)
        {
            yield return meleeFeedback.Play(target, onImpact, onApproachComplete, onReturnStart);
            yield break;
        }
        if (!isActiveAndEnabled || motionRoot == null || controller == null
            || motionRoot == controller.transform || !motionRoot.IsChildOf(controller.transform)
            || motionRoot.GetComponentInChildren<Collider>(true) != null)
        {
            onImpact?.Invoke();
            yield break;
        }
        if (target == null || !CanContinue()) yield break;

        _restPosition = motionRoot.localPosition;
        _playing = true;
        Vector3 direction = target.position - controller.transform.position;
        direction.y = 0f;
        float distance = Mathf.Min(Mathf.Max(0f, lungeDistance), direction.magnitude * 0.25f);
        Vector3 offset = motionRoot.parent.InverseTransformVector(direction.normalized * distance);
        var sequence = DOTween.Sequence().Pause();
        _sequence = sequence;
        try
        {
            sequence.Append(motionRoot.DOLocalMove(_restPosition + offset, Mathf.Max(0.01f, forwardTime))
                .SetEase(Ease.OutQuad));
            sequence.AppendCallback(() =>
            {
                if (target != null && CanContinue()) onImpact?.Invoke();
            });
            sequence.AppendInterval(Mathf.Max(0f, impactHoldTime));
            sequence.Append(motionRoot.DOLocalMove(_restPosition, Mathf.Max(0.01f, returnTime))
                .SetEase(Ease.OutSine));
            sequence.SetLink(motionRoot.gameObject, LinkBehaviour.KillOnDisable);
            sequence.OnKill(() =>
            {
                if (_sequence != sequence) return;
                _sequence = null;
                if (_playing && motionRoot != null) motionRoot.localPosition = _restPosition;
                _playing = false;
            });
            sequence.Play();

            while (sequence.IsActive())
            {
                // The struck enemy may disappear after impact; recovery must still finish.
                if (!CanContinue()) yield break;
                yield return null;
            }
        }
        finally
        {
            if (_sequence == sequence) StopAndRestore();
        }
    }

    private bool CanContinue()
    {
        return this != null && isActiveAndEnabled && motionRoot != null
            && motionRoot.gameObject.activeInHierarchy && controller != null
            && controller.data != null && !controller.isDead && controller.data.Hp > 0;
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private void OnDestroy()
    {
        StopAndRestore();
    }

    private void StopAndRestore()
    {
        if (meleeFeedback != null) meleeFeedback.Cancel();
        var sequence = _sequence;
        _sequence = null;
        sequence?.Kill();
        if (_playing && motionRoot != null) motionRoot.localPosition = _restPosition;
        _playing = false;
    }
}
