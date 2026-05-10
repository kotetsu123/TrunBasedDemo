using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Party Initial Data")]
public class PartyInitialData : ScriptableObject
{
    [SerializeField]private List<Character> members = new();

    public IReadOnlyList<Character>Members=> members;
}
