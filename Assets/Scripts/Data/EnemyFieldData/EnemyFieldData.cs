using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Game/Data/Enemy Field Data")]
public class EnemyFieldData : ScriptableObject
{
    [SerializeField] private string enemyId;
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private Character battleCharacterData;
    [SerializeField] private float wanderRadius = 3f;

    public string EnemyId=>enemyId;
    public GameObject FieldPrefab =>fieldPrefab;
    public Character BattleCharacterData =>battleCharacterData;
    public float WanderRadius => wanderRadius;  
}
