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
    [SerializeField] private float stuckRepathSeconds = 2f;
    [SerializeField] private float stuckCheckInterval = 0.5f;
    [SerializeField] private float stuckMinProgress = 0.1f;

    [Header("Chase")]
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float loseRadius = 7f;
    [SerializeField] private string playerTag = "Player";

    [Header("Chase Sight")]
    [SerializeField] private bool requireLineOfSightForChase = true;
    [SerializeField] private LayerMask chaseObstacleMask = 0;
    [SerializeField] private float chaseLineOfSightHeight = 0.8f;

    private Vector3 wanderCenter;
    private Vector3 wanderTarget;
    private float wanderRadius = 3f;
    private float waitTimer;
    private float stuckTimer;
    private float stuckCheckTimer;
    private float lastDistanceToWanderTarget = -1f;
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

        if (IsWanderTargetStuck())
        {
            PickNewWanderTarget();
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

        ResetWanderStuckCheck();
    }

    private bool IsWanderTargetStuck()
    {
        if (stuckRepathSeconds <= 0f)
            return false;

        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer < Mathf.Max(0.02f, stuckCheckInterval))
            return false;

        float currentDistance = FlatDistance(transform.position, wanderTarget);
        if (lastDistanceToWanderTarget < 0f)
        {
            lastDistanceToWanderTarget = currentDistance;
            stuckCheckTimer = 0f;
            return false;
        }

        // 如果距离没有明显变短，就认为这段时间可能被墙或障碍挡住了。
        float progress = lastDistanceToWanderTarget - currentDistance;
        if (progress < stuckMinProgress)
        {
            stuckTimer += stuckCheckTimer;
        }
        else
        {
            stuckTimer = 0f;
        }

        lastDistanceToWanderTarget = currentDistance;
        stuckCheckTimer = 0f;

        return stuckTimer >= stuckRepathSeconds;
    }

    private void ResetWanderStuckCheck()
    {
        stuckTimer = 0f;
        stuckCheckTimer = 0f;
        lastDistanceToWanderTarget = -1f;
    }

    private bool ShouldStartChase()
    {
        if (!canChasePlayer)
            return false;

        Transform player = GetPlayerTarget();
        if (player == null)
            return false;

        return FlatDistance(transform.position, player.position) <= detectRadius
               && HasLineOfSightToPlayer(player);
    }

    private bool CanKeepChasing()
    {
        if (!canChasePlayer)
            return false;

        Transform player = GetPlayerTarget();
        if (player == null)
            return false;

        return FlatDistance(transform.position, player.position) <= Mathf.Max(detectRadius, loseRadius)
               && HasLineOfSightToPlayer(player);
    }

    private Transform GetPlayerTarget()
    {
        if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
            return playerTarget;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        playerTarget = player != null ? player.transform : null;
        return playerTarget;
    }

    private bool HasLineOfSightToPlayer(Transform player)
    {
        if (!requireLineOfSightForChase)
            return true;

        if (player == null)
            return false;

        // 没有配置遮挡层时保持旧逻辑，避免敌人/玩家自己的 Collider 误挡索敌。
        if (chaseObstacleMask.value == 0)
            return true;

        Vector3 from = transform.position + Vector3.up * chaseLineOfSightHeight;
        Vector3 to = player.position + Vector3.up * chaseLineOfSightHeight;

        // 只检测墙体/环境层。中间有障碍时，敌人不会开始或继续追击玩家。
        return !Physics.Linecast(from, to, chaseObstacleMask, QueryTriggerInteraction.Ignore);
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

