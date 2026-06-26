using System;
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
    private static readonly Dictionary<string, DateTime> clearedSpawnUtcTimes = new Dictionary<string, DateTime>();
    private static readonly HashSet<string> openedChestIds = new HashSet<string>();

    //存档的数据相关。
    public static bool HasSavedPlayerTransform { get; private set; }
    public static Vector3 SavedPlayerPos { get; private set; }
    public static Quaternion SavedPlayerRot { get; private set; }

    public static IReadOnlyCollection<string> ClearedSpawnIds => clearedSpawnIds;
    public static IReadOnlyCollection<string> OpenedChestIds => openedChestIds;
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
        clearedSpawnUtcTimes[TriggeredSpawnId] = DateTime.UtcNow;

        Debug.Log($"[FieldBattleContext] Marked spawn ID as cleared: {TriggeredSpawnId}");
    }
    //检查某个spawnPoint的怪物是不是已经被打败过
    public static bool IsSpawnCleard(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return false;

        return clearedSpawnIds.Contains(spawnId);
    }
    //宝箱相关的记录，标记某个宝箱已经被打开过了
    public static void MarkChestOpened(string chestId)
    {
        if (string.IsNullOrWhiteSpace(chestId))
            return;

        openedChestIds.Add(chestId);
        Debug.Log($"[FieldBattleContext] Marked chest as opened: {chestId}");
    }

    public static bool IsChestOpened(string chestId)
    {
        if (string.IsNullOrWhiteSpace(chestId))
            return false;

        return openedChestIds.Contains(chestId);
    }
    // EnemySpawnManager 会在生成每个 SpawnPoint 前问这里：这个点现在要不要跳过生成？
    // 返回 true  = 这次不要生成。
    // 返回 false = 可以生成。
    public static bool ShouldSkipSpawn(string spawnId, bool canRespawn, float respawnSeconds)
    {
        // 没有 spawnId 的点无法被保存/清除记录管理，所以不要跳过，让它正常生成。
        if (string.IsNullOrWhiteSpace(spawnId))
            return false;

        // 这个 spawnId 没有被记录为已清除，说明它还没被打败过，可以正常生成。
        if (!clearedSpawnIds.Contains(spawnId))
            return false;

        // 已经被打败过，并且配置为不可刷新：这类就是 boss / 宝箱怪 / 一次性敌人。
        if (!canRespawn)
            return true;

        // 配置为可刷新，但刷新时间小于等于 0：视为立刻刷新，移除清除记录后允许生成。
        if (respawnSeconds <= 0f)
        {
            RemoveClearedSpawn(spawnId);
            return false;
        }

        // 可刷新小怪需要知道“上次是什么时候被打败的”。
        // 如果旧存档或异常情况里没有时间记录，就从现在开始重新计时，避免直接刷新。
        if (!clearedSpawnUtcTimes.TryGetValue(spawnId, out DateTime clearedAtUtc))
        {
            clearedAtUtc = DateTime.UtcNow;
            clearedSpawnUtcTimes[spawnId] = clearedAtUtc;
        }

        // 计算从被打败到现在经过了多少秒，用来和 SpawnPoint 表里的 RespawnSeconds 比较。
        double elapsedSeconds = (DateTime.UtcNow - clearedAtUtc).TotalSeconds;

        // 还没到刷新时间：继续跳过生成。
        if (elapsedSeconds < respawnSeconds)
            return true;

        // 到了刷新时间：把它从“已清除名单”里移除，然后允许这次生成。
        RemoveClearedSpawn(spawnId);
        Debug.Log($"[FieldBattleContext] Spawn respawned: {spawnId}");
        return false;
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


        foreach (string chestId in openedChestIds)
        {
            if (string.IsNullOrWhiteSpace(chestId))
                continue;

            saveData.openedChestIds.Add(chestId);
        }
        //HaseSet is good for runtime lookup, but save data uses List so JsonUtility can serialize it
        foreach(string spawnId in clearedSpawnIds)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                continue;

            saveData.clearedSpawnIds.Add(spawnId);

            DateTime clearedAtUtc = clearedSpawnUtcTimes.TryGetValue(spawnId, out DateTime storedTime)
                ? storedTime
                : DateTime.UtcNow;

            saveData.clearedSpawnRecords.Add(new FieldClearedSpawnSaveData
            {
                spawnId = spawnId,
                clearedAtUtc = clearedAtUtc.ToString("O")
            });
        }
        
        return saveData; 
    }

    public static void LoadFromSaveData(FieldSaveData saveData)
    {
        clearedSpawnIds.Clear();
        clearedSpawnUtcTimes.Clear();
        openedChestIds.Clear();
        ClearReturnData();
        EncounterCooldownUntilTime = 0f;

        HasSavedPlayerTransform = false;
        SavedPlayerPos = Vector3.zero;
        SavedPlayerRot = Quaternion.identity;

        if (saveData == null)
            return;

        if (saveData.openedChestIds != null)
        {
            foreach (string chestId in saveData.openedChestIds)
            {
                if (string.IsNullOrWhiteSpace(chestId))
                    continue;

                openedChestIds.Add(chestId);
            }
        }

        if (saveData.clearedSpawnRecords != null && saveData.clearedSpawnRecords.Count > 0)
        {
            foreach (FieldClearedSpawnSaveData record in saveData.clearedSpawnRecords)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.spawnId))
                    continue;

                clearedSpawnIds.Add(record.spawnId);

                if (DateTime.TryParse(record.clearedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime clearedAtUtc))
                    clearedSpawnUtcTimes[record.spawnId] = clearedAtUtc.ToUniversalTime();
                else
                    clearedSpawnUtcTimes[record.spawnId] = DateTime.UtcNow;
            }
        }

        if (saveData.clearedSpawnIds != null)
        {
            // Old save files only have IDs. Use now as their clear time for timed respawn compatibility.
            foreach (string spawnId in saveData.clearedSpawnIds)
            {
                if (string.IsNullOrWhiteSpace(spawnId))
                    continue;

                clearedSpawnIds.Add(spawnId);
                if (!clearedSpawnUtcTimes.ContainsKey(spawnId))
                    clearedSpawnUtcTimes[spawnId] = DateTime.UtcNow;
            }
        }
        //恢复玩家位置
        if (saveData.hasPlayerTransform)
        {
            HasSavedPlayerTransform = true;
            SavedPlayerPos = saveData.playerPos;
            SavedPlayerRot = Quaternion.Euler(saveData.playerRotEuler);
        }
    }
    public static void ClearSavedPlayerTransform()
    {
        HasSavedPlayerTransform = false;
        SavedPlayerPos = Vector3.zero;
        SavedPlayerRot = Quaternion.identity;
    }

    //用于重新开始流程或返回标题时完全情路
    public static void ClearAll()
    {
        ClearReturnData();
        ClearSavedPlayerTransform();
        clearedSpawnIds.Clear();
        clearedSpawnUtcTimes.Clear();
        openedChestIds.Clear();
        EncounterCooldownUntilTime = 0f;
    }

    private static void RemoveClearedSpawn(string spawnId)
    {
        clearedSpawnIds.Remove(spawnId);
        clearedSpawnUtcTimes.Remove(spawnId);
    }

}
