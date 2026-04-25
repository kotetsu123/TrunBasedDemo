using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    private bool triggerd;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerd) return;
        if (other.CompareTag("Player"))
        {
            triggerd = true;
            SceneManager.LoadScene(battleSceneName);
        }
    }
}
