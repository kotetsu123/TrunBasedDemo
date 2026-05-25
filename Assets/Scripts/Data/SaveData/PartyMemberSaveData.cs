using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PartyMemberSaveData 
{
    //用稳定ID 找回角色基础数据，不依赖Name
    public string characterId;

      // 保存当前运行时状态。
    public int hp;
    public int maxHp;
    public int mp;
    public int maxMp;

    public int level;
    public int exp;

    public bool isDead;
    
}
[System.Serializable]
public class PartySaveData
{
    // 按队伍顺序保存成员。List index 就是 party slot index。
    public List<PartyMemberSaveData> members=new List<PartyMemberSaveData>();
}
