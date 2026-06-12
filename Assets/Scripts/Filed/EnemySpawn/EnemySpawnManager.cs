using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;
    [SerializeField] private EnemyDataBase enemyDataBase;
    [SerializeField] private bool enableLiveRespawn = true;
    [SerializeField] private float liveRespawnCheckInterval = 1f;

    // 记录当前场景中每个 SpawnPoint 已经生成出来的敌人，避免同一个点重复生成多个小怪。
    private readonly Dictionary<string, EnemyFieldController> activeEnemiesBySpawnId = new Dictionary<string, EnemyFieldController>();
    private float nextLiveRespawnCheckTime;

    public void SetSpawnPoints(IReadOnlyList<EnemySpawnPoint> newSpawnPoints)
    {
        activeEnemiesBySpawnId.Clear();

        if (newSpawnPoints == null)
        {
            spawnPoints = new EnemySpawnPoint[0];
            return;
        }

        spawnPoints = new EnemySpawnPoint[newSpawnPoints.Count];
        for (int i = 0; i < newSpawnPoints.Count; i++)
        {
            spawnPoints[i] = newSpawnPoints[i];
        }
    }

    public void SpawnAll()
    {
        foreach (var point in spawnPoints)
        {
            TrySpawnPoint(point);
        }
    }

    private void Update()
    {
        if (!enableLiveRespawn)
            return;

        // 不需要每帧都检查刷新，按固定间隔检查就够了。
        if (Time.time < nextLiveRespawnCheckTime)
            return;

        nextLiveRespawnCheckTime = Time.time + Mathf.Max(0.1f, liveRespawnCheckInterval);
        CheckLiveRespawn();
    }

    private void CheckLiveRespawn()
    {
        foreach (var point in spawnPoints)
        {
            // Live Respawn 只处理表里设置为 Timed 的普通小怪，Permanent 继续保持 boss 式永久清除。
            if (point == null || !point.CanRespawn)
                continue;

            TrySpawnPoint(point);
        }
    }

    private void TrySpawnPoint(EnemySpawnPoint point)
    {
        if (point == null)
            return;
        if (!point.gameObject.activeInHierarchy)
            return;

        // 如果这个 SpawnPoint 当前已经有一个活着的 FieldEnemy，就不要重复生成。
        if (HasActiveEnemy(point.SpawnId))
            return;

        if (FieldBattleContext.ShouldSkipSpawn(point.SpawnId, point.CanRespawn, point.RespawnSeconds))
        {
            Debug.Log($"Skipping spawn for {point.SpawnId} due to field return context.");
            return;
        }

        string encounterId = point.EncounterId;
        GameObject fieldPrefab = point.FieldPrefab;
        float wanderRadius = point.WanderRadius;

        // Compatibility fallback: old SpawnPoints can still use enemyId to read Field prefab and encounterId.
        if ((fieldPrefab == null || string.IsNullOrWhiteSpace(encounterId)) &&
            !string.IsNullOrWhiteSpace(point.EnemyId))
        {
            EnemyFieldData enemyData = enemyDataBase != null ? enemyDataBase.FindById(point.EnemyId) : null;

            if (enemyData != null)
            {
                if (fieldPrefab == null)
                    fieldPrefab = enemyData.FieldPrefab;

                if (string.IsNullOrWhiteSpace(encounterId))
                    encounterId = enemyData.EncounterId;

                wanderRadius = enemyData.WanderRadius;
            }
        }

        if (fieldPrefab == null)
        {
            Debug.LogWarning($"[EnemySpawnManager] Field prefab is missing. spawnId={point.SpawnId}, encounterId={encounterId}, enemyId={point.EnemyId}");
            return;
        }

        if (string.IsNullOrWhiteSpace(encounterId))
        {
            Debug.LogWarning($"[EnemySpawnManager] EncounterId is missing. spawnId={point.SpawnId}, enemyId={point.EnemyId}");
            return;
        }

        GameObject enemy = Instantiate(
            fieldPrefab,
            point.transform.position,
            point.transform.rotation);

        EnemyFieldController fieldEnemy = enemy.GetComponent<EnemyFieldController>();

        if (fieldEnemy != null)
        {
            fieldEnemy.Init(
                point.SpawnId,
                encounterId,
                point.transform.position,
                wanderRadius);
        }

        RegisterActiveEnemy(point.SpawnId, fieldEnemy);
    }

    private bool HasActiveEnemy(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return false;

        if (!activeEnemiesBySpawnId.TryGetValue(spawnId, out EnemyFieldController enemy))
            return false;

        if (enemy != null && enemy.gameObject.activeInHierarchy)
            return true;

        activeEnemiesBySpawnId.Remove(spawnId);
        return false;
    }

    private void RegisterActiveEnemy(string spawnId, EnemyFieldController enemy)
    {
        if (string.IsNullOrWhiteSpace(spawnId) || enemy == null)
            return;

        activeEnemiesBySpawnId[spawnId] = enemy;
    }

}
