using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private SceneTransitionController transitionController;
    [SerializeField] private float groupEncounterRadius = 4f;

    private bool triggerd;

    private void Awake()
    {
        if (transitionController == null)
        {
            transitionController = FindObjectOfType<SceneTransitionController>();
        }
    }

    private void OnTriggerEnter(Collider other)
        
    {
        if (triggerd) return;

        if (FieldPauseState.IsPaused)
            return;

        if (FieldBattleContext.IsEncounterCooldownActive)
            return;
       
        if (other.CompareTag("Player"))
        {
            triggerd = true;
            
            EnemyFieldController fieldEnemy= GetComponent<EnemyFieldController>();
            List<string> spawnIds = new List<string>();
            List<string> encounterIds = new List<string>();
            CollectEncounterEnemies(fieldEnemy, spawnIds, encounterIds);

            FieldBattleContext.SaveFieldReturnData(SceneManager.GetActiveScene().name,        
                other.transform.position,
                other.transform.rotation,
                spawnIds,
                encounterIds);

            SimplePlayerMovement playerController = other.gameObject.GetComponent<SimplePlayerMovement>();
            Rigidbody playerRigidbody = other.gameObject.GetComponent<Rigidbody>();
            if (playerController != null)
            {
                playerRigidbody.velocity = Vector3.zero; // Í£Ö¹Íæ¼ÒÒÆ¶¯
                playerController.enabled = false;
                
            }

            if (transitionController != null)
            {
                transitionController.StartBattleTransition(battleSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
            }
        }
       
    }

    private void CollectEncounterEnemies(
        EnemyFieldController triggerEnemy,
        List<string> spawnIds,
        List<string> encounterIds)
    {
        AddEncounterEnemy(triggerEnemy, spawnIds, encounterIds);

        if (triggerEnemy == null || !triggerEnemy.IsChasing || groupEncounterRadius <= 0f)
            return;

        EnemyFieldController[] fieldEnemies = FindObjectsOfType<EnemyFieldController>();
        float sqrRadius = groupEncounterRadius * groupEncounterRadius;
        Vector3 center = triggerEnemy.transform.position;

        foreach (EnemyFieldController enemy in fieldEnemies)
        {
            if (enemy == null || enemy == triggerEnemy || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;

            if (offset.sqrMagnitude > sqrRadius)
                continue;

            AddEncounterEnemy(enemy, spawnIds, encounterIds);
        }

        Debug.Log($"[EncounterTrigger] Group encounter collected. spawnCount={spawnIds.Count}, encounterCount={encounterIds.Count}");
    }

    private void AddEncounterEnemy(
        EnemyFieldController enemy,
        List<string> spawnIds,
        List<string> encounterIds)
    {
        if (enemy == null)
            return;

        if (!string.IsNullOrWhiteSpace(enemy.SpawnId) && !spawnIds.Contains(enemy.SpawnId))
            spawnIds.Add(enemy.SpawnId);

        if (!string.IsNullOrWhiteSpace(enemy.EncounterId))
            encounterIds.Add(enemy.EncounterId);
    }
}

