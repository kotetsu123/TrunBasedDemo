using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemySpawnRespawnType
{
    Permanent,
    Timed
}

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId; // Unique spawn id used by FieldBattleContext and save data.
    [SerializeField] private string encounterId; // Table-driven battle id used by EncounterDataBase.
    [SerializeField] private GameObject fieldPrefab; // Field-side enemy prefab used for collision and wandering.
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private string enemyId; // Compatibility fallback for older EnemyDataBase driven spawn points.
    [SerializeField] private EnemySpawnRespawnType respawnType = EnemySpawnRespawnType.Permanent;
    [SerializeField] private float respawnSeconds = 60f;

    public string SpawnId => spawnId;
    public string EncounterId => encounterId;
    public GameObject FieldPrefab => fieldPrefab;
    public float WanderRadius => wanderRadius;
    public string EnemyId => enemyId;

    // Boss-like spawn points stay cleared forever. Timed spawn points can come back after RespawnSeconds.
    public bool CanRespawn => respawnType == EnemySpawnRespawnType.Timed;
    public float RespawnSeconds => Mathf.Max(0f, respawnSeconds);

    public void Configure(FieldSpawnPointEntry entry)
    {
        if (entry == null)
            return;

        spawnId = entry.SpawnId;
        encounterId = entry.EncounterId;
        fieldPrefab = entry.FieldPrefab;
        wanderRadius = entry.WanderRadius;
        enemyId = entry.EnemyId;
        respawnType = entry.RespawnType;
        respawnSeconds = entry.RespawnSeconds;

        transform.SetPositionAndRotation(entry.Position, entry.Rotation);
    }
}
