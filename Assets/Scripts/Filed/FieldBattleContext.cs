using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FieldBattleContext 
{
    private const float DefaultEncounterCooldownSeconds = 1.5f;

    public static string LastFieldSceneName { get; private set; }
    public static Vector3 PlayerPositionBeforeBattle { get; private set; }
    public static Quaternion PlayerRotationBeforeBattle { get; private set; }

    public static string TriggeredSpawnId { get; private set; }
    
    public static bool HasFieldReturnData { get; private set; }

    public static string CurrentEncounterId { get; private set;}

    public static float EncounterCooldownUntilTime { get; private set; }

    private static readonly HashSet<string> clearedSpawnIds= new HashSet<string>();

    public static IReadOnlyCollection<string> ClearedSpawnIds => clearedSpawnIds;
    public static bool IsEncounterCooldownActive => Time.time < EncounterCooldownUntilTime;
    //保存进入战斗前的FieldScene名称，玩家位置朝向
    public static void SaveFieldReturnData(string fieldSceneName,Vector3 playerPos,Quaternion playerRot,string triggeredSpawnId,string encounterId)
    {
        LastFieldSceneName = fieldSceneName;
        PlayerPositionBeforeBattle = playerPos;
        PlayerRotationBeforeBattle = playerRot;
        HasFieldReturnData = true;
        TriggeredSpawnId = triggeredSpawnId;
        CurrentEncounterId = encounterId;

        Debug.Log($"[FieldBattleContext] Saved return data: Scene={fieldSceneName}, spawnID={triggeredSpawnId},encounterID={encounterId}");
    }
    public static void StartEncounterCooldown(float seconds = DefaultEncounterCooldownSeconds)
    {
        EncounterCooldownUntilTime = Time.time + Mathf.Max(0f, seconds);

        Debug.Log($"[FieldBattleContext] Encounter cooldown started: {seconds}s");
    }

    //把刚才打败的怪物ID 记录到已击败名单里
    public static void MarkTriggerdEnemyCleared()
    {
        if (string.IsNullOrEmpty(TriggeredSpawnId))
            return;
        clearedSpawnIds.Add(TriggeredSpawnId);
            Debug.Log($"[FieldBattleContext] Marked spawn ID as cleared: {TriggeredSpawnId}");
    }
    //检查某个spawnPoint的怪物是不是已经被打败过
    public static bool IsSpawnCleard(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return false;

        return clearedSpawnIds.Contains(spawnId);
    }
    //只清理本次返回数据，不清理已击败敌人记录
    public static void ClearReturnData()
    {
        LastFieldSceneName = null;
        PlayerPositionBeforeBattle = Vector3.zero;
        PlayerRotationBeforeBattle = Quaternion.identity;
        TriggeredSpawnId = null;
        HasFieldReturnData = false;
        CurrentEncounterId = null;
    }
    public static FieldSaveData ToSaveData()
    {
        FieldSaveData saveData = new FieldSaveData();


        //HaseSet is good for runtime lookup, but save data uses List so JsonUtility can serialize it
        foreach(string spawnId in clearedSpawnIds)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                continue;

            saveData.clearedSpawnIds.Add(spawnId);
        }
        
        return saveData; 
    }

    public static void LoadFromSaveData(FieldSaveData saveData)
    {
        clearedSpawnIds.Clear();

        if (saveData == null || saveData.clearedSpawnIds == null)
            return;

        //Rebuild the runtime HashSet from the spawn IDs.

        foreach (string spawnId in saveData.clearedSpawnIds)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                continue;

            clearedSpawnIds.Add(spawnId);
        }
        ClearReturnData();
        EncounterCooldownUntilTime = 0f;
       

    }

    //用于重新开始流程或返回标题时完全情路
    public static void ClearAll()
    {
        ClearReturnData();
        clearedSpawnIds.Clear();
        EncounterCooldownUntilTime = 0f;
    }

}
