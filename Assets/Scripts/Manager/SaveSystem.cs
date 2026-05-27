using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveFileName = "save.json";

    //Application.persistentDataPath 是 Unity 给你的“适合存档的本地路径”。
    private static string SavePath=>Path.Combine(Application.persistentDataPath, SaveFileName);

    public static  GameSaveData BuildSaveData()
    {
        return new GameSaveData
        {
            inventory = InventoryRuntimeState.ToSaveData(),
            party = PartyRuntimeState.ToSaveData(),
        };
    }
    public static void Save()
    {
        GameSaveData saveData = BuildSaveData();
        string json = JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to {SavePath}");
    }
    public static bool Load(ItemDataBase itemDataBase,CharacterDataBase characterDataBase)
    {

    }
}
