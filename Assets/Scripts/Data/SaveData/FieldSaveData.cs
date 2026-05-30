using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FieldSaveData 
{
    //保存已经被击败的 SpawnPoint ID=> FiledBattleContext.ClearedSpawnIds
    //Load 后 EnemySpawnManager 会用这个Id 来判断哪些怪物不要重新生成。
    public List<string >clearedSpawnIds= new List<string>();
}
