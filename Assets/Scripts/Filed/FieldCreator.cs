using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour
{
    [SerializeField] private FieldData fieldData;
    [SerializeField] private Transform generatedSpawnPointRoot;
    [SerializeField] private Transform generatedInteractableRoot;
    [SerializeField] private Transform generatedEnvironmentRoot;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private EnemySpawnManager enemySpawnManager;
    [SerializeField] private CharacterDataBase characterDataBase;


   
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
        CreateRecruitPointsFromFieldData();
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
            spawnPointTransform.SetParent(GetSpawnPointRoot());

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

            FieldChestController chest = fieldObject.GetComponent<FieldChestController>();
            if (chest != null)
            {
                fieldObject.transform.SetParent(GetInteractableRoot());
                chest.Configure(entry.ObjectId);
            }
            else
            {
                fieldObject.transform.SetParent(GetEnvironmentRoot());
            }

            fieldObject.transform.localScale = objectScale;
        }
    }

    private void CreateRecruitPointsFromFieldData()
    {
        if (fieldData == null)
            return;

        foreach (FieldRecruitPointEntry entry in fieldData.RecruitPoints)
        {
            if (entry == null)
                continue;

            GameObject recruitPointObject = entry.PointPrefab != null
                ? Instantiate(entry.PointPrefab, entry.Position, entry.Rotation)
                : CreateDefaultRecruitPoint(entry);

            recruitPointObject.name = string.IsNullOrWhiteSpace(entry.RecruitId)
                ? $"RecruitPoint_{entry.CharacterId}"
                : entry.RecruitId;

            Vector3 pointScale = entry.Scale;
            if (pointScale == Vector3.zero)
            {
                Debug.LogWarning($"[FieldCreator] Recruit point scale is zero. Use Vector3.one instead. recruitId={entry.RecruitId}");
                pointScale = Vector3.one;
            }

            recruitPointObject.transform.localScale = pointScale;
            // RecruitPoint 是玩家可以按 E 互动的点，统一挂到 InteractableRoot 下方便和敌人 SpawnPoint 区分。
            recruitPointObject.transform.SetParent(GetInteractableRoot());

            FieldRecruitController recruitController = recruitPointObject.GetComponent<FieldRecruitController>();
            if (recruitController == null)
                recruitController = recruitPointObject.AddComponent<FieldRecruitController>();

            GameObject visualRoot = CreateRecruitVisual(entry, recruitPointObject.transform);

            recruitController.Configure(entry, characterDataBase, partyHudController, visualRoot);
        }
    }

    private GameObject CreateRecruitVisual(FieldRecruitPointEntry entry, Transform parent)
    {
        if (entry.VisualPrefab == null)
            return null;

        GameObject visualRoot = Instantiate(entry.VisualPrefab, parent);
        visualRoot.name = $"Visual_{entry.CharacterId}";
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        // FieldData 里配置的 visualPrefab 只应该负责显示。
        // 如果临时把完整 NPC prefab 当成 visual 使用，这里会关掉子物体上的交互脚本，避免生成两个入队点。
        FieldRecruitController[] nestedRecruitControllers = visualRoot.GetComponentsInChildren<FieldRecruitController>(true);
        foreach (FieldRecruitController nestedRecruitController in nestedRecruitControllers)
        {
            if (nestedRecruitController == null)
                continue;

            nestedRecruitController.enabled = false;
            Debug.LogWarning($"[FieldCreator] Visual prefab contains FieldRecruitController. Disabled nested controller. recruitId={entry.RecruitId}, characterId={entry.CharacterId}");
        }

        // visualPrefab 作为显示子物体时不负责触发交互；交互范围统一交给父级 RecruitPoint。
        Collider[] nestedColliders = visualRoot.GetComponentsInChildren<Collider>(true);
        foreach (Collider nestedCollider in nestedColliders)
        {
            if (nestedCollider == null)
                continue;

            nestedCollider.enabled = false;
        }

        return visualRoot;
    }

    private Transform GetSpawnPointRoot()
    {
        return generatedSpawnPointRoot != null ? generatedSpawnPointRoot : transform;
    }

    private Transform GetInteractableRoot()
    {
        if (generatedInteractableRoot != null)
            return generatedInteractableRoot;

        return GetSpawnPointRoot();
    }

    private Transform GetEnvironmentRoot()
    {
        if (generatedEnvironmentRoot != null)
            return generatedEnvironmentRoot;

        return transform;
    }

    private GameObject CreateDefaultRecruitPoint(FieldRecruitPointEntry entry)
    {
        GameObject recruitPointObject = new GameObject(string.IsNullOrWhiteSpace(entry.RecruitId)
            ? $"RecruitPoint_{entry.CharacterId}"
            : entry.RecruitId);

        recruitPointObject.transform.SetPositionAndRotation(entry.Position, entry.Rotation);

        CapsuleCollider trigger = recruitPointObject.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.75f;
        trigger.height = 2f;
        trigger.direction = 1;

        return recruitPointObject;
    }
}
