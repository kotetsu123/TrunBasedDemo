using System.Collections.Generic;

[System.Serializable]
public class TutorialSaveData
{
    // TutorialRuntimeState uses a HashSet for quick lookup,
    // but JsonUtility needs a serializable List.
    public List<string> completedTutorialIds = new List<string>();
}
