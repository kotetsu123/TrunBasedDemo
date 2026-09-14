using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestRewardData_", menuName = "Game Data/Chest Reward Data")]
public class ChestRewardData : ScriptableObject
{
    [SerializeField] private string chestId;
    [SerializeField] private List<InitialItemStack> rewards = new List<InitialItemStack>();

    public string ChestId => chestId;
    public IReadOnlyList<InitialItemStack> Rewards => rewards;
}
