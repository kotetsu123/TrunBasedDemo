using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;//生成点的唯一标识,用于记录该生成点的敌人是否已经被击败
    [SerializeField] private string encounterId;//新流程：直接指定遭遇战ID，用于从EncounterDatabase中生成敌人队伍
    [SerializeField] private GameObject fieldPrefab;//新流程：Field上显示/碰撞用的敌人Prefab
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private string enemyId;//敌人种类ID，用于从EnemyDatabase 中查找敌人配置


    public string SpawnId => spawnId;
    public string EncounterId => encounterId;
    public GameObject FieldPrefab => fieldPrefab;
    public float WanderRadius => wanderRadius;
    public string EnemyId=> enemyId;    
}
