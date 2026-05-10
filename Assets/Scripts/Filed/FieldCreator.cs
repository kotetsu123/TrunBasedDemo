using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private EnemySpawnManager enemySpawnManager;


    [SerializeField] private List<Character> InitialPartyMembers = new();
    [SerializeField] private PartyInitialData initialPartyData;

    [SerializeField]private FieldPartyHudController partyHudController;
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
