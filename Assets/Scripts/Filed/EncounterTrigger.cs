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
}
