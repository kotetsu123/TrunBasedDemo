using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField]private Vector3 offset=new Vector3(0,2,-6);
    [SerializeField] private float smoothTime = 0.05f;

    private Vector3 velocity ;

    [Header("Look")]
    [SerializeField]private float rotaionSpeed = 10f;

    [SerializeField] private SimplePlayerMovement player;


    private void LateUpdate()
    {
        //根据玩家输入调整平滑时间，也就是动态平衡
        //float currentSmooth = player.MoveInput.sqrMagnitude>0.01 ? 0.05f : 0.02f;
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
