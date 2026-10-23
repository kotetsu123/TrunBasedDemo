using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class SaveSlotInfo
{
    public int Slot { get; internal set; }
    public bool Exists { get; internal set; }
    public bool CanLoad => Data != null;
    public bool IsLegacy { get; internal set; }
    public bool UsesBackup { get; internal set; }
    public DateTime SavedAtUtc { get; internal set; }
    public string Error { get; internal set; }
    public GameSaveData Data { get; internal set; }
}

public static class SaveSystem
{
    public const int SlotCount = 5;
    public static string LastLoadedFieldSceneName { get; private set; }
    public static string LastError { get; private set; }
#if UNITY_EDITOR
    internal static string TestDirectory = null;
    private static string Root => TestDirectory ?? Application.persistentDataPath;
#else
    private static string Root => Application.persistentDataPath;
#endif
    private static string SlotPath(int slot) => Path.Combine(Root, $"save_{slot + 1:00}.json");
    private static string LegacyPath => Path.Combine(Root, "save.json");

    public static GameSaveData BuildSaveData()
    {
        FieldSaveData field = FieldBattleContext.ToSaveData();
        FieldSaveContext.Current?.TryFillFieldSaveData(field);
        return new GameSaveData
        {
            savedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            inventory = InventoryRuntimeState.ToSaveData(), party = PartyRuntimeState.ToSaveData(), field = field,
            tutorial = TutorialRuntimeState.ToSaveData(), fieldAutoEvents = FieldAutoEventRuntimeState.ToSaveData()
        };
    }

    // Preserve the original API for debug tools and existing integrations.
    public static bool Save() => SaveSlot(0);
    public static bool Load(ItemDataBase items, CharacterDataBase characters) => LoadSlot(0, items, characters);

    public static bool HasSaveFile()
    {
        for (int i = 0; i < SlotCount; i++) if (GetSlotInfo(i).Exists) return true;
        return false;
    }

    public static SaveSlotInfo[] GetSlots()
    {
        var slots = new SaveSlotInfo[SlotCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = GetSlotInfo(i);
        return slots;
    }

    public static SaveSlotInfo GetSlotInfo(int slot)
    {
        var info = new SaveSlotInfo { Slot = slot };
        if (slot < 0 || slot >= SlotCount) { info.Error = "Invalid save slot."; return info; }
        string path = SlotPath(slot);
        if (slot == 0 && !File.Exists(path) && !File.Exists(path + ".bak") && File.Exists(LegacyPath))
        { path = LegacyPath; info.IsLegacy = true; }
        info.Exists = File.Exists(path) || File.Exists(path + ".bak");
        if (!info.Exists) return info;
        if (!TryRead(path, out GameSaveData data, out string error))
        {
            if (!TryRead(path + ".bak", out data, out _)) { info.Error = error; return info; }
            path += ".bak";
            info.UsesBackup = true;
        }
        info.Data = data;
        if (DateTime.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTime saved)) info.SavedAtUtc = saved.ToUniversalTime();
        else
        {
            try { info.SavedAtUtc = File.GetLastWriteTimeUtc(path); }
            catch (IOException) { info.SavedAtUtc = DateTime.MinValue; }
        }
        return info;
    }

    public static bool SaveSlot(int slot)
    {
        LastError = null;
        if (slot < 0 || slot >= SlotCount) return Fail("Invalid save slot.");
        string path = SlotPath(slot), temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Root);
            string json = JsonUtility.ToJson(BuildSaveData(), true);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            // Never truncate a good save before the replacement is fully written.
            if (File.Exists(path))
            {
                bool valid = TryRead(path, out _, out _);
                File.Replace(temporary, path, valid ? path + ".bak" : null);
            }
            else File.Move(temporary, path);
            Debug.Log($"[SaveSystem] Saved slot {slot + 1}.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Save failed: {ex.Message}");
            return Fail("Could not write the save file. Your previous save was kept.");
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static bool LoadSlot(int slot, ItemDataBase items, CharacterDataBase characters, bool applyToCurrentScene = true)
    {
        LastError = null;
        var info = GetSlotInfo(slot);
        if (!info.CanLoad) return Fail(info.Error ?? "This save slot is empty.");
        GameSaveData data = info.Data;
        // Resolve required references before changing any live party or inventory state.
        if (!ValidateReferences(data, items, characters, out string error)) return Fail(error);
        string scene = data.field.sceneName;
        if (!string.IsNullOrWhiteSpace(scene) && !Application.CanStreamedLevelBeLoaded(scene))
            return Fail("The saved area is not available in this build.");
        InventoryRuntimeState.LoadFromSaveData(data.inventory, items);
        PartyRuntimeState.LoadFromSaveData(data.party, characters);
        FieldBattleContext.LoadFromSaveData(data.field);
        TutorialRuntimeState.LoadFromSaveData(data.tutorial);
        FieldAutoEventRuntimeState.LoadFromSaveData(data.fieldAutoEvents);
        LastLoadedFieldSceneName = scene;
        if (applyToCurrentScene)
        {
            FieldSaveContext.Current?.TryApplySavedPlayerTransform();
            FieldRecruitController.RefreshAllRecruitStates();
        }
        Debug.Log($"[SaveSystem] Loaded slot {slot + 1}.");
        return true;
    }

    private static bool TryRead(string path, out GameSaveData data, out string error)
    {
        data = null;
        error = null;
        try
        {
            if (!File.Exists(path)) { error = "Save file not found."; return false; }
            var candidate = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
            if (candidate == null || candidate.version != 1 || candidate.party?.members == null ||
                candidate.inventory?.slots == null || candidate.field == null)
            { error = "This save is damaged or uses an unsupported format."; return false; }
            if (candidate.party.members.Count > 64 || candidate.inventory.slots.Count > 10000)
            { error = "This save contains invalid data."; return false; }
            foreach (var member in candidate.party.members)
            {
                if (member == null || string.IsNullOrWhiteSpace(member.characterId)) continue;
                if (member.level < 1 || member.maxHp < 0 || member.maxMp < 0 ||
                    float.IsNaN(member.speed) || float.IsInfinity(member.speed))
                { error = "This save contains invalid character data."; return false; }
            }
            if (!Finite(candidate.field.playerPos) || !Finite(candidate.field.playerRotEuler))
            { error = "This save contains an invalid position."; return false; }
            data = candidate;
            return true;
        }
        catch (Exception) { error = "The save file could not be read."; return false; }
    }

    private static bool ValidateReferences(GameSaveData data, ItemDataBase items, CharacterDataBase characters, out string error)
    {
        var ids = new HashSet<string>();
        foreach (var member in data.party.members)
        {
            if (member == null || string.IsNullOrWhiteSpace(member.characterId)) continue;
            if (!ids.Add(member.characterId) || characters == null || characters.FindById(member.characterId) == null)
            { error = "A saved character is missing from the game data."; return false; }
        }
        foreach (var item in data.inventory.slots)
        {
            if (item == null || item.count <= 0) continue;
            if (string.IsNullOrWhiteSpace(item.itemId) || items == null || items.FindById(item.itemId) == null)
            { error = "A saved item is missing from the game data."; return false; }
        }
        error = null;
        return true;
    }

    private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
        !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    private static bool Fail(string error) { LastError = error; return false; }
    public static string GetLoadedFieldSceneNameOrDefault(string fallbackSceneName) =>
        !string.IsNullOrWhiteSpace(LastLoadedFieldSceneName) && Application.CanStreamedLevelBeLoaded(LastLoadedFieldSceneName)
        ? LastLoadedFieldSceneName : fallbackSceneName;
}
