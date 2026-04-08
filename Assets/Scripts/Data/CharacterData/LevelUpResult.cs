using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelUpResult
{
    public string characterName;
    public int beforeLevel;
    public int afterLevel;

    public bool DidLevelUp => afterLevel > beforeLevel;

    public LevelUpResult(string characterName,int beforeLevel,int afterLevel)
    {
        this.characterName = characterName;
        this.beforeLevel = beforeLevel;
        this.afterLevel = afterLevel;
    }

}
