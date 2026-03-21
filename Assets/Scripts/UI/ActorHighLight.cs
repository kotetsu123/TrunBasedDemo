using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActorHighLight : MonoBehaviour
{
    private Transform _target;
    private Renderer _cachedRenderer;
    private Camera _cam;
    private Tween _breathTween;

    [SerializeField] private Image image;
    public void Attach(Transform target)
    {
        if (_target == target)
            return;
        _target = target;
        if (_target != null)
        {
            _cachedRenderer = _target.GetComponentInChildren<Renderer>();
            _cam = Camera.main;
            gameObject.SetActive(true);
            StartBreath();
        }
        else
        {
            gameObject.SetActive(false);
            StopBreath();
        }

    }
    public void SetColor(Color color)
    {
        if (image != null)
        {
            image.color= color;
        }
    }
    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 worldPos = _target.position;
        worldPos.y -= 0.45f;
        //从身体中间的那个
        /*if (_cachedRenderer != null)
            worldPos = _cachedRenderer.bounds.center;*/

        transform.position = worldPos;

        if (_cam != null)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void StartBreath()
    {
        transform.localScale = Vector3.one;

        _breathTween?.Kill();

        _breathTween = transform
            .DOScale(1.08f, 0.9f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
    private void StopBreath()
    {
        _breathTween?.Kill();
    }
}
