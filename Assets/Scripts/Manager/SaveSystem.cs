using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveFileName = "save.json";

    // Unity provides persistentDataPath as a platform-safe folder for save files.
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    
    public static GameSaveData BuildSaveData()
    {
        
        FieldSaveData fieldSaveData = FieldBattleContext.ToSaveData();

        // FieldSaveContext only exists in Field scenes.
        // Title/Battle saves can still keep runtime field state, but cannot read a player Transform.
        FieldSaveContext.Current?.TryFillFieldSaveData(fieldSaveData);

        return new GameSaveData
        {
            inventory = InventoryRuntimeState.ToSaveData(),
            party = PartyRuntimeState.ToSaveData(),
            field=fieldSaveData,
        };
    }

    public static void Save()
    {
        GameSaveData saveData = BuildSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);

        Debug.Log($"[SaveSystem] Saved game to: {SavePath}");
    }

    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public static bool Load(ItemDataBase itemDataBase, CharacterDataBase characterDataBase)
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning($"[SaveSystem] Save file not found: {SavePath}");
            return false;
        }

        if (itemDataBase == null)
            Debug.LogWarning("[SaveSystem] ItemDataBase is null. Inventory items cannot be restored.");
        if (characterDataBase == null)
            Debug.LogWarning("[SaveSystem] CharacterDataBase is null. Party members cannot be restored.");

        string json = File.ReadAllText(SavePath);

        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (saveData == null)
        {
            Debug.LogWarning($"[SaveSystem] Failed to parse save file: {SavePath}");
            return false;
        }

        // Inventory needs ItemDataBase to convert saved itemId values back into ItemData.
        InventoryRuntimeState.LoadFromSaveData(saveData.inventory, itemDataBase);

        // Party needs CharacterDataBase to convert saved characterId values back into Character data.
        PartyRuntimeState.LoadFromSaveData(saveData.party, characterDataBase);

        //Field state restores cleared spawn IDs so defeated enemies do not respawn after Load.
        FieldBattleContext.LoadFromSaveData(saveData.field);

        // If Load is called while already in a Field scene, apply the saved player transform immediately.
        // Title Load has no FieldSaveContext, so FieldCreator will apply it after the Field scene loads.
        FieldSaveContext.Current?.TryApplySavedPlayerTransform();

        Debug.Log($"[SaveSystem] Loaded game from: {SavePath}");
        return true;
    }
}
