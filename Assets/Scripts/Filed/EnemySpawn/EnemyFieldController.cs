using UnityEngine;

public enum EnemyFieldState
{
    Wander,
    Chase
}

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
    private Vector3 wanderTarget;
    private float wanderRadius = 3f;
    private float waitTimer;
    private string spawnId;
    private string encounterId;
    private Transform playerTarget;
    private EnemyFieldState currentState = EnemyFieldState.Wander;

    public string SpawnId => spawnId;
    public string EncounterId => encounterId;
    public EnemyFieldState CurrentState => currentState;
    public bool IsChasing => currentState == EnemyFieldState.Chase;

    public void SetWanderCenter(Vector3 center, float radius)
    {
        wanderCenter = center;
        wanderRadius = radius;
        ResetWander();
    }

    public void Init(string id, string encounter, Vector3 center, float radius)
    {
        spawnId = id;
        encounterId = encounter;
        wanderCenter = center;
        wanderRadius = radius;
        ResetWander();
    }

    private void Update()
    {
        if (FieldPauseState.IsPaused)
            return;

        switch (currentState)
        {
            case EnemyFieldState.Wander:
                UpdateWander();
                break;
            case EnemyFieldState.Chase:
                UpdateChase();
                break;
        }
    }

    private void UpdateWander()
    {
        if (ShouldStartChase())
        {
            ChangeState(EnemyFieldState.Chase);
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        if (Vector3.Distance(transform.position, wanderTarget) <= 0.1f)
        {
            PickNewWanderTarget();
            waitTimer = waitTime;
            return;
        }

        MoveTowards(wanderTarget, moveSpeed);
    }

    private void UpdateChase()
    {
        if (!CanKeepChasing())
        {
            ResetWander();
            ChangeState(EnemyFieldState.Wander);
            return;
        }

        Vector3 target = playerTarget.position;
        target.y = transform.position.y;
        MoveTowards(target, chaseSpeed);
    }

    private void ChangeState(EnemyFieldState nextState)
    {
        if (currentState == nextState)
            return;

        currentState = nextState;
    }

    private void ResetWander()
    {
        PickNewWanderTarget();
        waitTimer = 0f;
        ChangeState(EnemyFieldState.Wander);
    }

    public void ResetToWander()
    {
        ResetWander();
    }

    private void PickNewWanderTarget()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        wanderTarget = new Vector3(
            wanderCenter.x + random.x,
            transform.position.y,
            wanderCenter.z + random.y);
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

