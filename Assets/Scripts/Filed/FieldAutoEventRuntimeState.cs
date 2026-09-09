using System.Collections.Generic;
using UnityEngine;

public static class FieldAutoEventRuntimeState
{
    private static readonly HashSet<string> completedEventIds = new HashSet<string>();

    public static bool IsCompleted(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        return completedEventIds.Contains(eventId);
    }

    public static void MarkCompleted(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return;

        if (completedEventIds.Add(eventId))
            Debug.Log($"[FieldAutoEventRuntimeState] Auto event completed: {eventId}");
    }

    public static FieldAutoEventSaveData ToSaveData()
    {
        FieldAutoEventSaveData saveData = new FieldAutoEventSaveData();

        foreach (string eventId in completedEventIds)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                continue;

            saveData.completedEventIds.Add(eventId);
        }

        return saveData;
    }

    public static void LoadFromSaveData(FieldAutoEventSaveData saveData)
    {
        completedEventIds.Clear();

        if (saveData?.completedEventIds == null)
            return;

        foreach (string eventId in saveData.completedEventIds)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                continue;

            completedEventIds.Add(eventId);
        }

        Debug.Log($"[FieldAutoEventRuntimeState] Loaded completed auto events: {completedEventIds.Count}");
    }

    public static void Clear()
    {
        completedEventIds.Clear();
    }
}

[System.Serializable]
public class FieldAutoEventSaveData
{
    // FieldAutoEventRuntimeState uses a HashSet for quick lookup,
    // but JsonUtility needs a serializable List.
    public List<string> completedEventIds = new List<string>();
}
