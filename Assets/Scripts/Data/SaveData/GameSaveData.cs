[System.Serializable]
public class TutorialSaveData
{
    // TutorialRuntimeState uses a HashSet for quick lookup,
    // but JsonUtility needs a serializable List.
    public System.Collections.Generic.List<string> completedTutorialIds = new System.Collections.Generic.List<string>();
}

[System.Serializable]
public class GameSaveData
{
    // Save schema version. This gives us a place to handle migration if the save format changes later.
    public int version = 1;

    // Runtime snapshots that can be serialized together by SaveSystem.
    public InventorySaveData inventory;
    public PartySaveData party;
    public FieldSaveData field;
    public TutorialSaveData tutorial;
}

public static class TutorialRuntimeState
{
    private static readonly System.Collections.Generic.HashSet<string> completedTutorialIds = new System.Collections.Generic.HashSet<string>();

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
            UnityEngine.Debug.Log($"[TutorialRuntimeState] Tutorial completed: {tutorialId}");
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

        UnityEngine.Debug.Log($"[TutorialRuntimeState] Loaded completed tutorials: {completedTutorialIds.Count}");
    }

    public static void Clear()
    {
        completedTutorialIds.Clear();
    }
}
