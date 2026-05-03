using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private EnemySpawnManager enemySpawnManager;

    private void Start()
    {
        SetupPlayer();
        SpwanEnemies();
        if (FieldBattleContext.HasFieldReturnData)
        {
            FieldBattleContext.Clear();
        }
    }
    private void SetupPlayer()
    {
        if (player == null || playerStartPoint == null)
            return;
        if (FieldBattleContext.HasFieldReturnData)
        {
            player.position = FieldBattleContext.PlayerPositionBeforeBattle;
            player.rotation = FieldBattleContext.PlayerRotationBeforeBattle;
            return;
        }
        if (playerStartPoint != null)
        {
            player.position = playerStartPoint.position;
            player.rotation = playerStartPoint.rotation;
        }
       
    }

    private void SpwanEnemies()
    {
        enemySpawnManager?.SpawnAll();
    }
}
