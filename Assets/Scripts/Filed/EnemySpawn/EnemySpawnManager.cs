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

            GameObject enemy = Instantiate(
                point.EnemyPrefab,
                point.transform.position,
                point.transform.rotation);

            EnemyFieldController fieldEnemy = enemy.GetComponent <EnemyFieldController>();

            if (fieldEnemy != null)
            {
                fieldEnemy.SetWanderCenter(
                    point.transform.position,
                    point.WanderRadius);
            }
        }
    }

}
