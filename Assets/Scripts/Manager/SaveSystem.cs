using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveFileName = "save.json";

    // Unity provides persistentDataPath as a platform-safe folder for save files.
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static string LastLoadedFieldSceneName { get; private set; }
    
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
            tutorial = TutorialRuntimeState.ToSaveData(),
        };
    }

    public static bool Save()
    {
        try
        {
            GameSaveData saveData = BuildSaveData();
            string json = JsonUtility.ToJson(saveData, true);

            File.WriteAllText(SavePath, json);

            Debug.Log($"[SaveSystem] Saved game to: {SavePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
            return false;
        }
    }

    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public static bool Load(ItemDataBase itemDataBase, CharacterDataBase characterDataBase)
    {
        LastLoadedFieldSceneName = null;

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

        LastLoadedFieldSceneName = saveData.field?.sceneName;

        // Inventory needs ItemDataBase to convert saved itemId values back into ItemData.
        InventoryRuntimeState.LoadFromSaveData(saveData.inventory, itemDataBase);

        // Party needs CharacterDataBase to convert saved characterId values back into Character data.
        PartyRuntimeState.LoadFromSaveData(saveData.party, characterDataBase);

        //Field state restores cleared spawn IDs so defeated enemies do not respawn after Load.
        FieldBattleContext.LoadFromSaveData(saveData.field);

        // Tutorial state keeps one-time tutorials from playing again after Load.
        TutorialRuntimeState.LoadFromSaveData(saveData.tutorial);

        // If Load is called while already in a Field scene, apply the saved player transform immediately.
        // Title Load has no FieldSaveContext, so FieldCreator will apply it after the Field scene loads.
        FieldSaveContext.Current?.TryApplySavedPlayerTransform();

        // Loading can roll back party members. Refresh recruit triggers so NPCs reappear when their character is no longer in party.
        FieldRecruitController.RefreshAllRecruitStates();

        Debug.Log($"[SaveSystem] Loaded game from: {SavePath}");
        return true;
    }

    public static string GetLoadedFieldSceneNameOrDefault(string fallbackSceneName)
    {
        if (string.IsNullOrWhiteSpace(LastLoadedFieldSceneName))
            return fallbackSceneName;

        if (!Application.CanStreamedLevelBeLoaded(LastLoadedFieldSceneName))
        {
            Debug.LogWarning($"[SaveSystem] Saved field scene cannot be loaded: {LastLoadedFieldSceneName}. Fallback={fallbackSceneName}");
            return fallbackSceneName;
        }

        return LastLoadedFieldSceneName;
    }
}
