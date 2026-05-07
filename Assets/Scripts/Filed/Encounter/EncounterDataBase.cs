using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Encounter DataBase")]
public class EncounterDataBase : ScriptableObject {
    [SerializeField] private List<EncounterData> encounters=new List<EncounterData>();

    public EncounterData FindeById(string encounterId)
    {
        foreach(var encounter in encounters)
        {
            if (encounter == null)
                continue;
            if (encounter.EncounterId == encounterId)
                return encounter;
        }
        Debug.LogWarning($"[EncounterDataBase] EncounterID not found:{encounterId}");
        return null;
    }
}
