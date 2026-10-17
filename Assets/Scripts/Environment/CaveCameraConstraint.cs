using UnityEngine;

/// <summary>Keeps the field camera inside the authored cave after its normal follow update.</summary>
[DefaultExecutionOrder(90)]
public sealed class CaveCameraConstraint : MonoBehaviour
{
    public Transform target;
    public Camera viewCamera;
    public LayerMask obstacleMask = 1 << 7;
    [Min(0.1f)] public float radius = 0.28f;
    [Min(0f)] public float surfacePadding = 0.08f;

    void LateUpdate()
    {
        if (!viewCamera) viewCamera = Camera.main;
        if (!target)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }
        if (!target || !viewCamera) return;

        Vector3 focus = target.position + Vector3.up * 1.2f;
        viewCamera.transform.position = ResolvePosition(focus, viewCamera.transform.position);
        if ((viewCamera.transform.position - focus).sqrMagnitude > 0.001f)
            viewCamera.transform.LookAt(focus);
    }

    public Vector3 ResolvePosition(Vector3 focus, Vector3 desired)
    {
        Vector3 offset = desired - focus;
        float distance = offset.magnitude;
        if (distance < 0.001f) return desired;
        // Include the near-plane corners, so a wide camera cannot slice into the wall.
        float clearance = radius;
        if (viewCamera)
        {
            float halfHeight = viewCamera.nearClipPlane * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            clearance = Mathf.Max(clearance, halfHeight * Mathf.Sqrt(1 + viewCamera.aspect * viewCamera.aspect));
        }
        if (Physics.SphereCast(focus, clearance, offset / distance, out RaycastHit hit,
            distance, obstacleMask, QueryTriggerInteraction.Ignore))
            return focus + offset / distance * Mathf.Max(0.01f, hit.distance - surfacePadding);
        return desired;
    }
}
