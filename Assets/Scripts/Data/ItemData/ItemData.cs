using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Item Data")]

public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemtype;
    public int power;
}
