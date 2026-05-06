using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;//生成点的唯一标识,用于记录该生成点的敌人是否已经被击败
    [SerializeField] private string enemyId;//敌人种类ID，用于从EnemyDatabase 中查找敌人配置


    public string SpawnId => spawnId;
    public string EnemyId=> enemyId;    
}
