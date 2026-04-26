using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField]private Vector3 offset=new Vector3(0,5,-6);
    [SerializeField] private float smoothTime = 0.3f;//越大跟的越慢
    private Vector3 velocity ;

   // [Header("Mouse Rotation")]

    [Header("Look")]
    [SerializeField]private float rotaionSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;
        //目标位置
        Vector3 targetPos=target.position + offset;

        //平滑移动
        transform.position=Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
            );

        //平滑朝向
        Quaternion targetRot=Quaternion.LookRotation(target.position - transform.position);
        transform.rotation=Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotaionSpeed * Time.deltaTime
            );
    }
}
