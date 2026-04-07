using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterResultSnapshot 
{
    public string Name;
    public Sprite portrait;

    public int hp;
    public int maxhp;
    public int mp;
    public int maxmp;

    public int level;
    public int exp;//当前等级内经验
    public int expToNextLevel;  //当前等级升到下一等级需要的经验
}
public class BattleResultPayload
{
    public BattleResult Result { get; }
    public IReadOnlyList<CharacterResultSnapshot> PartySnapshots { get; }

    public BattleResultPayload(BattleResult result,List<CharacterResultSnapshot> partySnapshots)
    {
        this.Result = result;
        this.PartySnapshots = partySnapshots;
    }
}
