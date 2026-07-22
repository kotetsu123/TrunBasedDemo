using UnityEngine;

public class SimplePlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 moveInput;

    public Vector3 MoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (FieldPauseState.IsPaused)
        {
            StopHorizontalMovement();
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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
        if (FieldPauseState.IsPaused)
        {
            StopHorizontalMovement();
            return;
        }

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 move = moveInput * moveSpeed;
            rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            Quaternion smoothRot = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                10f * Time.fixedDeltaTime);
            rb.MoveRotation(smoothRot);
        }
        else
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }

    private void StopHorizontalMovement()
    {
        moveInput = Vector3.zero;

        if (rb == null)
            return;

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
    }
}
