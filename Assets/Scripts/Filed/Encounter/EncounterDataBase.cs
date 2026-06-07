using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Encounter DataBase")]
public class EncounterDataBase : ScriptableObject {
    [SerializeField] private List<EncounterData> encounters=new List<EncounterData>();

    public EncounterData FindeById(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
        {
            Debug.LogWarning("[EncounterDataBase] EncounterId is empty.");
            return null;
        }

        EncounterData found = null;

        foreach(var encounter in encounters)
        {
            if (encounter == null)
                continue;

            encounter.ValidateConfig();

            if (encounter.EncounterId != encounterId)
                continue;

            if (found != null)
            {
                Debug.LogWarning($"[EncounterDataBase] Duplicate EncounterId found: {encounterId}. first={found.name}, duplicate={encounter.name}");
                continue;
            }

            found = encounter;
        }

        if (found != null)
            return found;

        Debug.LogWarning($"[EncounterDataBase] EncounterID not found:{encounterId}");
        return null;
    }
}
