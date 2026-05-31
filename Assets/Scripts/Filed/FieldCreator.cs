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
        FieldPauseState.Clear();

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
            FieldBattleContext.StartEncounterCooldown();
            FieldBattleContext.ClearReturnData();
        }

       
    }
    private void SetupPlayer()
    {
        if (player == null)
            return;
        // Battle return has the highest priority because it is an immediate scene round trip.
        if (FieldBattleContext.HasFieldReturnData)
        {
            FieldPlayerTransformUtility.Teleport(
                player,
                FieldBattleContext.PlayerPositionBeforeBattle,
                FieldBattleContext.PlayerRotationBeforeBattle);
            return;
        }
        // Saved player transform is used when entering Field from Load.
        if (FieldBattleContext.HasSavedPlayerTransform)
        {
            FieldPlayerTransformUtility.Teleport(
                player,
                FieldBattleContext.SavedPlayerPos,
                FieldBattleContext.SavedPlayerRot);
            FieldBattleContext.ClearSavedPlayerTransform();
            return;
        }
        // New Game or no saved transform: use the scene start point.
        if (playerStartPoint != null)
        {
            FieldPlayerTransformUtility.Teleport(
                player,
                playerStartPoint.position,
                playerStartPoint.rotation);
        }
    }

    private void SpwanEnemies()
    {
        enemySpawnManager?.SpawnAll();
    }
}
