using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;//生成点的唯一标识
    [SerializeField] private GameObject enmeyPrefab;
    [SerializeField] private float wanderRadius = 3f;//允许怪物游荡的范围


    public string SpawnId => spawnId;
    public GameObject EnemyPrefab => enmeyPrefab;
    public float WanderRadius => wanderRadius;
}
