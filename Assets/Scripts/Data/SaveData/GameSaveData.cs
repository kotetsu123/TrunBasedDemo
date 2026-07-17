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

