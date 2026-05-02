using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject enmeyPrefab;
    [SerializeField] private float wanderRadius = 3f;//允许怪物游荡的范围

    public GameObject EnemyPrefab => enmeyPrefab;
    public float WanderRadius => wanderRadius;
}
