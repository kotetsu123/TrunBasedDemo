using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetCircle : MonoBehaviour
{
    private Transform _target;
    private Renderer _cachedRenderer;
    private Camera _cam;


    public void Attach(Transform target)
    {
        _target = target;
        if (_target != null)
        {
            _cachedRenderer = _target.GetComponentInChildren<Renderer>();
            _cam = Camera.main;
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    private void LateUpdate()
    {
        if (_target == null) return;

       Vector3 worldPos=_target.position;

        if (_cachedRenderer != null)
            worldPos = _cachedRenderer.bounds.center;

        transform.position = worldPos;
        
        //ÈÃui ³¯ÏòÏà»ú
        if(_cam!=null)
            transform.forward=_cam.transform.forward;

    }

}
