using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [SerializeField] private string encounterId;
    [SerializeField] private List<Character> enemyChatacters = new List<Character>();

    public string EncounterId => encounterId;
    public List<Character> EnemyChatacters => enemyChatacters;
}
