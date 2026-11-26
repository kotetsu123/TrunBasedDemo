using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(20)]
[RequireComponent(typeof(SimplePlayerMovement), typeof(Rigidbody))]
public sealed class FieldLocomotionVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float strideSpeed = 1.5f;
    [SerializeField, Min(1f)] private float maxRunRate = 1.18f;

    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int RunRate = Animator.StringToHash("RunRate");
    private SimplePlayerMovement movement;
    private Rigidbody body;
    private Vector3 previousPosition;
    private float horizontalSpeed;

    private void Awake()
    {
        movement = GetComponent<SimplePlayerMovement>();
        body = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        previousPosition = body.position;
        horizontalSpeed = 0f;
    }

    private void FixedUpdate()
    {
        Vector3 delta = body.position - previousPosition;
        previousPosition = body.position;
        delta.y = 0f;
        horizontalSpeed = delta.magnitude / Time.fixedDeltaTime;
    }

    private void Update()
    {
        bool running = !FieldPauseState.IsPaused && movement.isActiveAndEnabled
            && movement.MoveInput.sqrMagnitude > 0.01f && horizontalSpeed > 0.08f;
        animator.SetBool(Moving, running);
        if (running)
            animator.SetFloat(RunRate, Mathf.Clamp(horizontalSpeed / strideSpeed, 0.65f, maxRunRate));
    }
}
