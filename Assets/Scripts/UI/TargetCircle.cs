using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TargetCircle : MonoBehaviour
{
    private Transform _target;
    private Renderer _cachedRenderer;
    private Camera _cam;
    private Tween _breathTween;
    

    public void Attach(Transform target)
    {
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
    private void LateUpdate()
    {
        if (_target == null) return;

       Vector3 worldPos=_target.position;

        if (_cachedRenderer != null)
            worldPos = _cachedRenderer.bounds.center;

        transform.position = worldPos;
        
        //让ui 朝向相机
        if(_cam!=null)
            transform.forward=_cam.transform.forward;

    }
    private void StartBreath()
    {
        transform.localScale= Vector3.one;

        _breathTween?.Kill();

        _breathTween = transform
            .DOScale(1.1f, 0.8f)
            .SetEase(Ease.InOutSine)//控制动画“速度曲线”
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopBreath()
    {
        _breathTween?.Kill();
    }


}
