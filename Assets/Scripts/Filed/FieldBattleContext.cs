using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FieldBattleContext 
{
    public static string LastFieldSceneName { get; private set; }
    public static Vector3 PlayerPositionBeforeBattle { get; private set; }
    public static Quaternion PlayerRotationBeforeBattle { get; private set; }

    public static string TriggeredSpawnId { get; private set; }
    
    public static bool HasFieldReturnData { get; private set; }

    private static readonly HashSet<string> clearedSpawnIds= new HashSet<string>();

    public static IReadOnlyCollection<string> ClearedSpawnIds => clearedSpawnIds;
    public static void SaveFieldReturnData(string fieldSceneName,Vector3 playerPos,Quaternion playerRot,string triggeredSpawnId)
    {
        LastFieldSceneName = fieldSceneName;
        PlayerPositionBeforeBattle = playerPos;
        PlayerRotationBeforeBattle = playerRot;
        HasFieldReturnData = true;
        TriggeredSpawnId = triggeredSpawnId;

        Debug.Log($"[FieldBattleContext] Saved return data: Scene={fieldSceneName}, Position={playerPos}, Rotation={playerRot}");
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
    public static void ClearReturnData()
    {
        LastFieldSceneName = null;
        PlayerPositionBeforeBattle = Vector3.zero;
        PlayerRotationBeforeBattle = Quaternion.identity;
        TriggeredSpawnId = null;
        HasFieldReturnData = false;
    }
    public static void ClearAll()
    {
        ClearReturnData();
        clearedSpawnIds.Clear();
    }
}
