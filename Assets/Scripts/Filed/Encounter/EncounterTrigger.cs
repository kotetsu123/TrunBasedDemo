using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private SceneTransitionController transitionController;
    [Header("Group Encounter")]
    [SerializeField] private float groupEncounterRadius = 4f;
    [SerializeField] private bool requireLineOfSightForGroup = true;
    [SerializeField] private LayerMask groupObstacleMask = 0;
    [SerializeField] private float groupLineOfSightHeight = 0.8f;
    [SerializeField] private Color groupRadiusGizmoColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    [SerializeField] private Color groupLinkGizmoColor = new Color(1f, 0.45f, 0.1f, 0.9f);

    [Header("Runtime Group Link")]
    [SerializeField] private bool showGroupLinksInGame = true;
    [SerializeField] private bool showGroupLinksOnlyWhileChasing = true;
    [SerializeField] private float groupLinkRefreshInterval = 0.1f;
    [SerializeField] private float groupLinkHeight = 1.2f;
    [SerializeField] private float groupLinkWidth = 0.04f;
    [SerializeField] private Color groupRuntimeLinkColor = new Color(1f, 0.45f, 0.1f, 0.9f);
    [SerializeField] private Material groupLinkMaterial;

    private readonly List<LineRenderer> groupLinkRenderers = new List<LineRenderer>();
    private readonly List<EnemyFieldController> groupLinkTargets = new List<EnemyFieldController>();
    private bool triggerd;
    private float nextGroupLinkRefreshTime;
    private Material runtimeGroupLinkMaterial;
    private EnemyFieldController cachedFieldEnemy;

    private void Awake()
    {
        cachedFieldEnemy = GetComponent<EnemyFieldController>();

        if (transitionController == null)
        {
            transitionController = FindObjectOfType<SceneTransitionController>();
        }
    }


    private void Update()
    {
        RefreshGroupEncounterLinkTargets();
    }

    private void LateUpdate()
    {
        UpdateGroupEncounterLinkPositions();
    }

    private void OnDisable()
    {
        HideAllGroupLinks();
    }

    private void RefreshGroupEncounterLinkTargets()
    {
        if (!showGroupLinksInGame || groupEncounterRadius <= 0f)
        {
            HideAllGroupLinks();
            groupLinkTargets.Clear();
            return;
        }

        if (!CanShowGroupLinks())
        {
            HideAllGroupLinks();
            groupLinkTargets.Clear();
            return;
        }

        // ���Ҹ������˱ȽϹ�����ֻ�����ˢ��Ŀ���б���
        // ������λ�û��� LateUpdate ÿ֡���£��ƶ�ʱ��ȵ�һ���˳��
        if (Time.time < nextGroupLinkRefreshTime)
            return;

        nextGroupLinkRefreshTime = Time.time + Mathf.Max(0.02f, groupLinkRefreshInterval);
        groupLinkTargets.Clear();

        EnemyFieldController[] fieldEnemies = FindObjectsOfType<EnemyFieldController>();
        float sqrRadius = groupEncounterRadius * groupEncounterRadius;

        foreach (EnemyFieldController enemy in fieldEnemies)
        {
            if (enemy == null || enemy == cachedFieldEnemy || !enemy.gameObject.activeInHierarchy)
                continue;

            if (!CanJoinGroupEncounter(cachedFieldEnemy, enemy, sqrRadius))
                continue;

            groupLinkTargets.Add(enemy);
        }

        HideGroupLinksFromIndex(groupLinkTargets.Count);
    }

    private void UpdateGroupEncounterLinkPositions()
    {
        if (!showGroupLinksInGame || groupEncounterRadius <= 0f || !CanShowGroupLinks())
        {
            HideAllGroupLinks();
            return;
        }

        Vector3 center = GetRuntimeLinkPoint(cachedFieldEnemy.transform);
        int visibleLineCount = 0;

        for (int i = 0; i < groupLinkTargets.Count; i++)
        {
            EnemyFieldController target = groupLinkTargets[i];
            if (target == null || !target.gameObject.activeInHierarchy)
                continue;

            LineRenderer line = GetOrCreateGroupLinkRenderer(visibleLineCount);
            line.gameObject.SetActive(true);
            line.SetPosition(0, center);
            line.SetPosition(1, GetRuntimeLinkPoint(target.transform));
            visibleLineCount++;
        }

        HideGroupLinksFromIndex(visibleLineCount);
    }

    private bool CanShowGroupLinks()
    {
        if (cachedFieldEnemy == null)
            cachedFieldEnemy = GetComponent<EnemyFieldController>();

        if (cachedFieldEnemy == null)
            return false;

        if (showGroupLinksOnlyWhileChasing && !cachedFieldEnemy.IsChasing)
            return false;

        return true;
    }
    private Vector3 GetRuntimeLinkPoint(Transform target)
    {
        return target.position + Vector3.up * groupLinkHeight;
    }

    private LineRenderer GetOrCreateGroupLinkRenderer(int index)
    {
        while (groupLinkRenderers.Count <= index)
        {
            GameObject lineObject = new GameObject($"GroupEncounterLink_{groupLinkRenderers.Count}");
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = groupLinkWidth;
            line.endWidth = groupLinkWidth;
            line.startColor = groupRuntimeLinkColor;
            line.endColor = groupRuntimeLinkColor;
            line.material = GetGroupLinkMaterial();
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            lineObject.SetActive(false);

            groupLinkRenderers.Add(line);
        }

        LineRenderer renderer = groupLinkRenderers[index];
        renderer.startWidth = groupLinkWidth;
        renderer.endWidth = groupLinkWidth;
        renderer.startColor = groupRuntimeLinkColor;
        renderer.endColor = groupRuntimeLinkColor;
        renderer.material = GetGroupLinkMaterial();
        return renderer;
    }

    private Material GetGroupLinkMaterial()
    {
        if (groupLinkMaterial != null)
            return groupLinkMaterial;

        if (runtimeGroupLinkMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            runtimeGroupLinkMaterial = new Material(shader);
        }

        return runtimeGroupLinkMaterial;
    }

    private void HideAllGroupLinks()
    {
        HideGroupLinksFromIndex(0);
    }

    private void HideGroupLinksFromIndex(int startIndex)
    {
        for (int i = startIndex; i < groupLinkRenderers.Count; i++)
        {
            if (groupLinkRenderers[i] == null)
                continue;

            groupLinkRenderers[i].gameObject.SetActive(false);
        }
    }
    private void OnTriggerEnter(Collider other)
        
    {
        if (triggerd) return;

        if (FieldPauseState.IsPaused)
            return;

        if (other.CompareTag("Player"))
        {
            if (FieldBattleContext.IsEncounterCooldownActive)
            {
                // 冷却时间内只把敌人从追击状态拉回游荡，不进入战斗。
                // 这相当于 Run/Escape 返回 Field 后的短暂无敌帧。
                EnemyFieldController cooldownEnemy = GetComponent<EnemyFieldController>();
                cooldownEnemy?.ResetToWander();
                return;
            }

            triggerd = true;
            
            EnemyFieldController fieldEnemy= GetComponent<EnemyFieldController>();
            List<string> spawnIds = new List<string>();
            List<string> encounterIds = new List<string>();
            CollectEncounterEnemies(fieldEnemy, spawnIds, encounterIds);

            FieldBattleContext.SaveFieldReturnData(SceneManager.GetActiveScene().name,        
                other.transform.position,
                other.transform.rotation,
                spawnIds,
                encounterIds);

            SimplePlayerMovement playerController = other.gameObject.GetComponent<SimplePlayerMovement>();
            Rigidbody playerRigidbody = other.gameObject.GetComponent<Rigidbody>();
            if (playerController != null)
            {
                playerRigidbody.velocity = Vector3.zero; // ֹͣ����ƶ�
                playerController.enabled = false;
                
            }

            if (transitionController != null)
            {
                transitionController.StartBattleTransition(battleSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
            }
        }
       
    }

    private void CollectEncounterEnemies(
        EnemyFieldController triggerEnemy,
        List<string> spawnIds,
        List<string> encounterIds)
    {
        AddEncounterEnemy(triggerEnemy, spawnIds, encounterIds);

        if (triggerEnemy == null || !triggerEnemy.IsChasing || groupEncounterRadius <= 0f)
            return;

        EnemyFieldController[] fieldEnemies = FindObjectsOfType<EnemyFieldController>();
        float sqrRadius = groupEncounterRadius * groupEncounterRadius;

        foreach (EnemyFieldController enemy in fieldEnemies)
        {
            if (enemy == null || enemy == triggerEnemy || !enemy.gameObject.activeInHierarchy)
                continue;

            if (!CanJoinGroupEncounter(triggerEnemy, enemy, sqrRadius))
                continue;

            AddEncounterEnemy(enemy, spawnIds, encounterIds);
        }

        Debug.Log($"[EncounterTrigger] Group encounter collected. spawnCount={spawnIds.Count}, encounterCount={encounterIds.Count}");
    }

    private bool CanJoinGroupEncounter(EnemyFieldController sourceEnemy, EnemyFieldController targetEnemy, float sqrRadius)
    {
        if (sourceEnemy == null || targetEnemy == null)
            return false;

        Vector3 offset = targetEnemy.transform.position - sourceEnemy.transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude > sqrRadius)
            return false;

        if (!requireLineOfSightForGroup)
            return true;

        return HasGroupLineOfSight(sourceEnemy.transform, targetEnemy.transform);
    }

    private bool HasGroupLineOfSight(Transform source, Transform target)
    {
        if (source == null || target == null)
            return false;

        // 没有配置遮挡层时保持旧逻辑，避免误把敌人/玩家 Collider 当成墙挡住。
        if (groupObstacleMask.value == 0)
            return true;

        Vector3 from = source.position + Vector3.up * groupLineOfSightHeight;
        Vector3 to = target.position + Vector3.up * groupLineOfSightHeight;

        // 只检测墙体/环境层。中间有障碍物时，这个敌人不会加入联合遇敌。
        return !Physics.Linecast(from, to, groupObstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void AddEncounterEnemy(
        EnemyFieldController enemy,
        List<string> spawnIds,
        List<string> encounterIds)
    {
        if (enemy == null)
            return;

        if (!string.IsNullOrWhiteSpace(enemy.SpawnId) && !spawnIds.Contains(enemy.SpawnId))
            spawnIds.Add(enemy.SpawnId);

        if (!string.IsNullOrWhiteSpace(enemy.EncounterId))
            encounterIds.Add(enemy.EncounterId);
    }

    private void OnDrawGizmosSelected()
    {
        if (groupEncounterRadius <= 0f)
            return;

        EnemyFieldController triggerEnemy = GetComponent<EnemyFieldController>();
        if (triggerEnemy == null)
            return;

        Vector3 center = transform.position;

        Gizmos.color = groupRadiusGizmoColor;
        Gizmos.DrawWireSphere(center, groupEncounterRadius);

        EnemyFieldController[] fieldEnemies = FindObjectsOfType<EnemyFieldController>();
        float sqrRadius = groupEncounterRadius * groupEncounterRadius;

        Gizmos.color = groupLinkGizmoColor;

        foreach (EnemyFieldController enemy in fieldEnemies)
        {
            if (enemy == null || enemy == triggerEnemy || !enemy.gameObject.activeInHierarchy)
                continue;

            if (!CanJoinGroupEncounter(triggerEnemy, enemy, sqrRadius))
                continue;

            Gizmos.DrawLine(center, enemy.transform.position);
            Gizmos.DrawSphere(enemy.transform.position + Vector3.up * 0.4f, 0.15f);
        }
    }
}




