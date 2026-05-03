using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFieldController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1f;

    private Vector3 wanderCenter;
    private float wanderRadius = 3f;
    private string spawnId;

    public string SpawnId=> spawnId;
    public void SetWanderCenter(Vector3 center,float radius)
    {
        wanderCenter= center;
        wanderRadius= radius;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
    }
    public void Init(string id,Vector3 center,float radius)
    {
        spawnId= id;
        wanderCenter= center;
        wanderRadius= radius;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
    }
    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            {
                Vector3 target = GetRandomPoint();
                while (Vector3.Distance(transform.position, target) > 0.1f)
                {
                    Vector3 dir = (target - transform.position).normalized;

                    transform.position += dir * moveSpeed * Time.deltaTime;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(dir);
                    }
                    yield return null;
                }
                yield return new WaitForSeconds(waitTime);
            }
        }
    }

    private Vector3 GetRandomPoint()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;

        return new Vector3(wanderCenter.x+random.x,
            transform.position.y,
            wanderCenter.z+random.y);
    }
}
