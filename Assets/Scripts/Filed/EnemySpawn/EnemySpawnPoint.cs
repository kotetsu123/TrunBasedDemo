using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;//生成点的唯一标识
    [SerializeField] private string enemyId;


    public string SpawnId => spawnId;
    public string EnemyId=> enemyId;    
}
