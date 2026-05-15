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
        {
            PartyRuntimeState.InitializeIfEmpty(initialPartyData.Members);
        }
        SetupPlayer();
        SpwanEnemies();

        partyHudController?.Refresh();

        if (FieldBattleContext.HasFieldReturnData)
        {
            FieldBattleContext.ClearReturnData();
        }

        foreach (var stack in initialItems)
        {
            if (stack == null)
                continue;
            
            InventoryRuntimeState.AddItem(stack.item, stack.count);
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
