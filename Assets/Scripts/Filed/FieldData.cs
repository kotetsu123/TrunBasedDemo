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

[System.Serializable]
public class FieldObjectEntry
{
    [SerializeField] private string objectId;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector3 position;
    [SerializeField] private Vector3 rotationEuler;
    [SerializeField] private Vector3 scale = Vector3.one;

    public string ObjectId => objectId;
    public GameObject Prefab => prefab;
    public Vector3 Position => position;
    public Quaternion Rotation => Quaternion.Euler(rotationEuler);
    public Vector3 Scale => scale;
}

[CreateAssetMenu(fileName = "FieldData_", menuName = "Game Data/Field Data")]
public class FieldData : ScriptableObject
{
    [SerializeField] private string fieldId;
    [SerializeField] private List<FieldSpawnPointEntry> spawnPoints = new List<FieldSpawnPointEntry>();
    [SerializeField] private List<FieldObjectEntry> fieldObjects = new List<FieldObjectEntry>();

    public string FieldId => fieldId;
    public IReadOnlyList<FieldSpawnPointEntry> SpawnPoints => spawnPoints;
    public IReadOnlyList<FieldObjectEntry> FieldObjects => fieldObjects;
}
