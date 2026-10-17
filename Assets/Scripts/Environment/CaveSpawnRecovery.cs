using System.Collections;
using UnityEngine;

/// <summary>Old field saves may predate the carved passages. Recover only points outside this cave's floor.</summary>
public sealed class CaveSpawnRecovery : MonoBehaviour
{
    [SerializeField] private Vector3[] safePositions = new Vector3[0];
    [System.NonSerialized] private MeshCollider[] surfaces;
    private float nextCheck;
    private Transform playerTransform;
    private bool ready;

    public void Configure(Vector3[] positions) { safePositions = positions; }

    IEnumerator Start()
    {
        // FieldCreator applies scene-return/save transforms during Start.
        yield return null;
        var player = GameObject.FindGameObjectWithTag("Player");
        playerTransform=player?player.transform:null;
        ready=true;
        CheckPosition();
    }

    void LateUpdate()
    {
        if(!ready || Time.unscaledTime<nextCheck) return;
        nextCheck=Time.unscaledTime+.5f;
        CheckPosition();
    }

    void CheckPosition()
    {
        if (!playerTransform || HasFloor(playerTransform.position)) return;
        if (TryFindRecovery(playerTransform.position, out Vector3 destination))
        {
            FieldPlayerTransformUtility.Teleport(playerTransform, destination, playerTransform.rotation);
            Debug.Log("[Cave] Old field position was outside the remodeled tunnels; moved to the nearest safe passage. Save data was not changed.");
        }
        else Debug.LogError("[Cave] No safe recovery position is configured.");
    }

    public bool HasFloor(Vector3 position)
    {
        if (surfaces == null || surfaces.Length==0 || !surfaces[0]) surfaces = GetComponentsInChildren<MeshCollider>();
        var ray = new Ray(new Vector3(position.x,1.25f,position.z),Vector3.down);
        foreach (var surface in surfaces)
            if (surface && surface.enabled && surface.Raycast(ray,out RaycastHit hit,1.5f) &&
                hit.normal.y > .8f && Mathf.Abs(hit.point.y-.012f)<.1f) return true;
        return false;
    }

    public bool TryFindRecovery(Vector3 from, out Vector3 destination)
    {
        destination=from;
        float nearest=float.PositiveInfinity;
        foreach(var candidate in safePositions)
        {
            float distance=(candidate-from).sqrMagnitude;
            if(distance>=nearest || !HasFloor(candidate)) continue;
            if(Physics.CheckBox(candidate+Vector3.up*.35f,new Vector3(.56f,.3f,.56f),Quaternion.identity,1<<7,QueryTriggerInteraction.Ignore)) continue;
            nearest=distance; destination=candidate;
        }
        return !float.IsPositiveInfinity(nearest);
    }
}
