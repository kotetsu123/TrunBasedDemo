using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;
    [SerializeField] private EnemyDataBase enemyDataBase;

    public void SpawnAll()
    {
        foreach (var point in spawnPoints)
        {
            if (point == null)
                continue;
            if (!point.gameObject.activeInHierarchy)
                continue;

            if (FieldBattleContext.ShouldSkipSpawn(point.SpawnId, point.CanRespawn, point.RespawnSeconds))
            {
                Debug.Log($"Skipping spawn for {point.SpawnId} due to field return context.");
                continue;
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
                continue;
            }

            if (string.IsNullOrWhiteSpace(encounterId))
            {
                Debug.LogWarning($"[EnemySpawnManager] EncounterId is missing. spawnId={point.SpawnId}, enemyId={point.EnemyId}");
                continue;
            }

            
            GameObject enemy = Instantiate(
                fieldPrefab,
                point.transform.position,
                point.transform.rotation);

            EnemyFieldController fieldEnemy = enemy.GetComponent <EnemyFieldController>();

            if (fieldEnemy != null)
            {
                fieldEnemy.Init(
                    point.SpawnId,
                    encounterId,
                    point.transform.position,
                    wanderRadius);
            }
        }
    }

}
