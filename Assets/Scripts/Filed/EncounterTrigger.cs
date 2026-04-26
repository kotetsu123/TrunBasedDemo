using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private SceneTransitionController transitionController;

    private bool triggerd;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerd) return;
        if (other.CompareTag("Player"))
        {
            triggerd = true;
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
}
