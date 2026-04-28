using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float height = 3f;
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Rotation")]
    [SerializeField] private float yaw = 0f; //左右转（水平旋转）
    [SerializeField] private float pitch = 25f; //上下转（垂直旋转/俯视角）
    [SerializeField]private float rotateSpeed = 3f;
    [SerializeField]private float minPitch = -15f;
    [SerializeField]private float maxPitch = 60f;

    private Vector3 followVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        HandleCameraRotation();
        FollowTarget();
    }
    private void HandleCameraRotation()
    {
        if (!Input.GetMouseButton(1)) return;// 右键旋转

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * rotateSpeed;
        pitch -= mouseY * rotateSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    private void FollowTarget()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset = rotation * new Vector3(0f, height, -distance);
        Vector3 targetPosition = target.position + offset;

        
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, smoothTime);
        transform.LookAt(target.position+Vector3.up*1.2f);
    }
}
