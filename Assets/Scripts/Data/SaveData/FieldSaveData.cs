using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FieldSaveData 
{
    //保存已经被击败的 SpawnPoint ID=> FiledBattleContext.ClearedSpawnIds
    //Load 后 EnemySpawnManager 会用这个Id 来判断哪些怪物不要重新生成。
    public List<string >clearedSpawnIds= new List<string>();

    //保存玩家所在的field场景名。
    //之后多地图时，Title Load 可以根据这个字段回到正确场景。
    public string sceneName;

    //保存玩家在field 中的位置
    public Vector3 playerPos;

    //保存玩家在field 中的朝向 ,用Euler 角保存，方便在save.json中查看
    public Vector3 playerRotEuler;
    
    //避免旧存档或者未记录位置时误用默认 vector3.zero
    public bool hasPlayerTransform;
    
}
