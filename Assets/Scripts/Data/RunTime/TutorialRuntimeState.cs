using System.Collections.Generic;
using UnityEngine;

public static class TutorialRuntimeState
{
    private static readonly HashSet<string> completedTutorialIds = new HashSet<string>();

    public static bool IsCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return false;

        return completedTutorialIds.Contains(tutorialId);
    }

    public static void MarkCompleted(string tutorialId)
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            return;

        if (completedTutorialIds.Add(tutorialId))
            Debug.Log($"[TutorialRuntimeState] Tutorial completed: {tutorialId}");
    }

    public static TutorialSaveData ToSaveData()
    {
        TutorialSaveData saveData = new TutorialSaveData();

        foreach (string tutorialId in completedTutorialIds)
        {
            if (string.IsNullOrWhiteSpace(tutorialId))
                continue;

            saveData.completedTutorialIds.Add(tutorialId);
        }

        return saveData;
    }

    public static void LoadFromSaveData(TutorialSaveData saveData)
    {
        completedTutorialIds.Clear();

        if (saveData?.completedTutorialIds == null)
            return;

        foreach (string tutorialId in saveData.completedTutorialIds)
        {
            if (string.IsNullOrWhiteSpace(tutorialId))
                continue;

            completedTutorialIds.Add(tutorialId);
        }

        Debug.Log($"[TutorialRuntimeState] Loaded completed tutorials: {completedTutorialIds.Count}");
    }

    public static void Clear()
    {
        completedTutorialIds.Clear();
    }
}
