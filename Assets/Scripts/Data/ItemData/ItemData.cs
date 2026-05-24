using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Item Data")]

public class ItemData : ScriptableObject
{
    // 保存/读取时使用的稳定 ID。之后存档里会保存 itemId，而不是直接保存 ScriptableObject 引用。
    public string itemId;

    public string itemName;
    public ItemType itemtype;
    public int power;

    public Sprite icon;

    [TextArea]
    public string description;

    private void OnValidate()
    {
        // 旧的 ItemData 资产可能还没有手动填写 itemId。
        // 先用 asset name 自动补一个初始值，之后如果需要更稳定的命名可以在 Inspector 里手动改。
        if (string.IsNullOrWhiteSpace(itemId))
            itemId = name;
    }
}
