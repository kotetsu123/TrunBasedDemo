using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 moveInput;


    public Vector3 MoveInput=> moveInput;
    private void Awake()
    {
        rb=GetComponent<Rigidbody>();
    }

    private void Update()
    {
        //暂停判断
        if (FieldPauseState.IsPaused)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        moveInput = (cameraForward * v + cameraRight * h).normalized;
        
    }

    private void FixedUpdate()
    {
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 move = moveInput * moveSpeed;
            rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

            Quaternion targetRot=Quaternion.LookRotation(moveInput);
            Quaternion smoothRot=Quaternion.Slerp(
                transform.rotation,
                targetRot,
                10f*Time.fixedDeltaTime);
            rb.MoveRotation(smoothRot);
        }
        else
        {// 界岺彊틱盧땡，뎃괏넣뉩殮醵똑（흔契禿）꼇긴
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }
}
