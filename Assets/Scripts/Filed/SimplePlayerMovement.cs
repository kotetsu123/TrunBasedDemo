using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 moveInput;


    public Vector3 MoveInput=> moveInput;
    private void Awake()
    {
        rb=GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        moveInput=new Vector3(h,0f,v).normalized;
        
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
        {// 停止水平移动，但保持垂直速度（如跳跃）不变
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }
}
