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

            if (FieldBattleContext.IsSpawnCleard(point.SpawnId))
            {
                Debug.Log($"Skipping spawn for {point.SpawnId} due to field return context.");
                continue;
            }
            EnemyFieldData enemyData = enemyDataBase.FindById(point.EnemyId);

            if (enemyData == null || enemyData.FieldPrefab == null)         
                continue;

            
            GameObject enemy = Instantiate(
                enemyData.FieldPrefab,
                point.transform.position,
                point.transform.rotation);

            EnemyFieldController fieldEnemy = enemy.GetComponent <EnemyFieldController>();

            if (fieldEnemy != null)
            {
                fieldEnemy.Init(
                    point.SpawnId,
                    point.transform.position,
                    enemyData.WanderRadius);
            }
        }
    }

}
