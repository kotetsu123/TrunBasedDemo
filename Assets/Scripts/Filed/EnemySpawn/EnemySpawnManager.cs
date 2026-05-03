using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;

    public void SpawnAll()
    {
        foreach (var point in spawnPoints)
        {
            if (point == null || point.EnemyPrefab == null)
                continue;

            if (FieldBattleContext.HasFieldReturnData && point.SpawnId == FieldBattleContext.TriggeredSpawnId)
            {
                Debug.Log($"Skipping spawn for {point.SpawnId} due to field return context.");
                continue;
            }

            GameObject enemy = Instantiate(
                point.EnemyPrefab,
                point.transform.position,
                point.transform.rotation);

            EnemyFieldController fieldEnemy = enemy.GetComponent <EnemyFieldController>();

            if (fieldEnemy != null)
            {
                fieldEnemy.Init(
                    point.SpawnId,
                    point.transform.position,
                    point.WanderRadius);
            }
        }
    }

}
