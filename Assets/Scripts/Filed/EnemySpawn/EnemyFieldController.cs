using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFieldController : MonoBehaviour
{
    [Header("Wander")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waitTime = 1f;

    [Header("Chase")]
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float loseRadius = 7f;
    [SerializeField] private string playerTag = "Player";

    private Vector3 wanderCenter;
    private float wanderRadius = 3f;
    private string spawnId;
    private string encounterId;
    private Transform playerTarget;

    public string SpawnId=> spawnId;
    public string EncounterId => encounterId;   
    public void SetWanderCenter(Vector3 center,float radius)
    {
        wanderCenter= center;
        wanderRadius= radius;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
    }
    public void Init(string id,string encounter,Vector3 center,float radius)
    {
        spawnId= id;
        encounterId= encounter;
        wanderCenter= center;
        wanderRadius= radius;

        StopAllCoroutines();
        StartCoroutine(WanderRoutine());
    }
    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (FieldPauseState.IsPaused)
            {
                yield return null;
                continue;
            }
            if (ShouldStartChase())
            {
                yield return ChaseRoutine();
                continue;
            }

            Vector3 target = GetRandomPoint();
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                if (FieldPauseState.IsPaused)
                {
                    yield return null;
                    continue;
                }
                if (ShouldStartChase())
                {
                    yield return ChaseRoutine();
                    break;
                }

                MoveTowards(target, moveSpeed);
                yield return null;
            }
            yield return new WaitForSeconds(waitTime);
        }
    }
    private IEnumerator ChaseRoutine()
    {
        while (CanKeepChasing())
        {
            if (FieldPauseState.IsPaused)
            {
                yield return null;
                continue;
            }

            Vector3 target = playerTarget.position;
            target.y = transform.position.y;

            MoveTowards(target, chaseSpeed);
            yield return null;
        }
    }

    private Vector3 GetRandomPoint()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;

        return new Vector3(wanderCenter.x+random.x,
            transform.position.y,
            wanderCenter.z+random.y);
    }

    private bool ShouldStartChase()
    {
        if (!canChasePlayer)
            return false;

        Transform player = GetPlayerTarget();
        if (player == null)
            return false;

        return FlatDistance(transform.position, player.position) <= detectRadius;
    }

    private bool CanKeepChasing()
    {
        if (!canChasePlayer)
            return false;

        Transform player = GetPlayerTarget();
        if (player == null)
            return false;

        return FlatDistance(transform.position, player.position) <= Mathf.Max(detectRadius, loseRadius);
    }

    private Transform GetPlayerTarget()
    {
        if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            return playerTarget;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        playerTarget = player != null ? player.transform : null;
        return playerTarget;
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector3 dir = direction.normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
