using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterResultSnapshot 
{
    public string characterName;
    public Sprite portrait;

    public int hp;
    public int maxhp;
    public int mp;
    public int maxmp;

    public int level;
    public int exp;//当前经验（总经验）
    public int expToNextLevel;  //升级所需经验（到下一级）
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
