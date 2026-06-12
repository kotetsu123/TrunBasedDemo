using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FieldSpawnPointEntry
{
    [SerializeField] private string spawnId;
    [SerializeField] private string encounterId;
    [SerializeField] private GameObject fieldPrefab;
    [SerializeField] private Vector3 position;
    [SerializeField] private Vector3 rotationEuler;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private string enemyId;
    [SerializeField] private EnemySpawnRespawnType respawnType = EnemySpawnRespawnType.Permanent;
    [SerializeField] private float respawnSeconds = 60f;

    public string SpawnId => spawnId;
    public string EncounterId => encounterId;
    public GameObject FieldPrefab => fieldPrefab;
    public Vector3 Position => position;
    public Quaternion Rotation => Quaternion.Euler(rotationEuler);
    public float WanderRadius => wanderRadius;
    public string EnemyId => enemyId;
    public EnemySpawnRespawnType RespawnType => respawnType;
    public float RespawnSeconds => respawnSeconds;
}

[CreateAssetMenu(fileName = "FieldData_", menuName = "Game Data/Field Data")]
public class FieldData : ScriptableObject
{
    [SerializeField] private string fieldId;
    [SerializeField] private List<FieldSpawnPointEntry> spawnPoints = new List<FieldSpawnPointEntry>();

    public string FieldId => fieldId;
    public IReadOnlyList<FieldSpawnPointEntry> SpawnPoints => spawnPoints;
}
