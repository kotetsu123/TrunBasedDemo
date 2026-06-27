using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour
{
    [SerializeField] private FieldData fieldData;
    [SerializeField] private Transform generatedSpawnPointRoot;
    [SerializeField] private Transform generatedObjectRoot;
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

        CreateSpawnPointsFromFieldData();
        CreateObjectsFromFieldData();
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

    private void CreateSpawnPointsFromFieldData()
    {
        if (fieldData == null || enemySpawnManager == null)
            return;

        List<EnemySpawnPoint> generatedSpawnPoints = new List<EnemySpawnPoint>();

        foreach (FieldSpawnPointEntry entry in fieldData.SpawnPoints)
        {
            if (entry == null)
                continue;

            GameObject spawnPointObject = new GameObject($"SpawnPoint_{entry.SpawnId}");
            Transform spawnPointTransform = spawnPointObject.transform;
            spawnPointTransform.SetParent(generatedSpawnPointRoot != null ? generatedSpawnPointRoot : transform);

            EnemySpawnPoint spawnPoint = spawnPointObject.AddComponent<EnemySpawnPoint>();
            spawnPoint.Configure(entry);
            generatedSpawnPoints.Add(spawnPoint);
        }

        enemySpawnManager.SetSpawnPoints(generatedSpawnPoints);
    }

    private void CreateObjectsFromFieldData()
    {
        if (fieldData == null)
            return;

        foreach (FieldObjectEntry entry in fieldData.FieldObjects)
        {
            if (entry == null)
                continue;

            if (entry.Prefab == null)
            {
                Debug.LogWarning($"[FieldCreator] Field object prefab is missing. fieldId={fieldData.FieldId}, objectId={entry.ObjectId}");
                continue;
            }

            // This is a lightweight entry point for data-driven field objects.
            // Heavy 3D environment art can still stay scene-authored, while gameplay objects can be generated here.
            GameObject fieldObject = Instantiate(entry.Prefab, entry.Position, entry.Rotation);
            fieldObject.name = string.IsNullOrWhiteSpace(entry.ObjectId)
                ? entry.Prefab.name
                : entry.ObjectId;
            // Validate scale to avoid zero scale issues.
            Vector3 objectScale = entry.Scale;
            if(objectScale == Vector3.zero)
            {
                Debug.LogWarning($"[FieldCreator] Field object scale is zero. Use Vector3.one instead. objectId={entry.ObjectId}");
                objectScale = Vector3.one;
            }

            fieldObject.transform.localScale = objectScale;
            fieldObject.transform.SetParent(generatedObjectRoot != null ? generatedObjectRoot : transform);

            FieldChestController chest = fieldObject.GetComponent<FieldChestController>();
            if (chest != null)
                chest.Configure(entry.ObjectId);
        }
    }
}
