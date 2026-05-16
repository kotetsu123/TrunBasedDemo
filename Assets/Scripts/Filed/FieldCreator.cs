using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private EnemySpawnManager enemySpawnManager;


   
    [SerializeField] private PartyInitialData initialPartyData;

    [SerializeField]private FieldPartyHudController partyHudController;

    [SerializeField] private List<InitialItemStack> initialItems = new();
    private void Start()
    {
        if (initialPartyData != null)
        {//第一次进入没有初始化。所以添加初始化道具
            PartyRuntimeState.InitializeIfEmpty(initialPartyData.Members);
        }
        //之后返回 已经初始化过。跳过初始化。
        InventoryRuntimeState.InitializeIfEmpty(initialItems);

        SetupPlayer();
        SpwanEnemies();

        partyHudController?.Refresh();

        if (FieldBattleContext.HasFieldReturnData)
        {
            FieldBattleContext.ClearReturnData();
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
