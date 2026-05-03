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

    public static void SaveFieldReturnData(string fieldSceneName,Vector3 playerPos,Quaternion playerRot,string triggeredSpawnId)
    {
        LastFieldSceneName = fieldSceneName;
        PlayerPositionBeforeBattle = playerPos;
        PlayerRotationBeforeBattle = playerRot;
        HasFieldReturnData = true;
        TriggeredSpawnId = triggeredSpawnId;

        Debug.Log($"[FieldBattleContext] Saved return data: Scene={fieldSceneName}, Position={playerPos}, Rotation={playerRot}");
    }

    public static void Clear()
    {
        LastFieldSceneName = null;
        PlayerPositionBeforeBattle = Vector3.zero;
        PlayerRotationBeforeBattle = Quaternion.identity;
        TriggeredSpawnId = null;
        HasFieldReturnData = false;
    }
}
