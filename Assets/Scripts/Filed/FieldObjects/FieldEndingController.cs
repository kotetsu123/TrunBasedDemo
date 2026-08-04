using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldEndingController : MonoBehaviour
{
    [SerializeField] private DialogueData endingDialogue;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Finish";
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private DialoguePanelController dialoguePanel;

    private bool isPlayerInRange;
    private bool isPlayingEnding;

    // Start is called before the first frame update
    void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if(dialoguePanel==null)
            dialoguePanel = DialoguePanelController.Current;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPlayerInRange || isPlayingEnding)
            return;
        if (Input.GetKeyDown(interactKey))
            StartEnding();
    }
    private void StartEnding()
    {
        if (isPlayingEnding)
            return;

        if(dialoguePanel==null)
            dialoguePanel = DialoguePanelController.Current;

        if (dialoguePanel == null)
        {
            Debug.LogWarning($"[FieldEnding] DialoguePanelController is missing. object={gameObject.name}");
            return;
        }

        if (endingDialogue == null)
        {
            Debug.LogWarning($"[FieldEnding] Ending DialogueData is missing. object={gameObject.name}");
            return;
        }
        isPlayingEnding = true;
        HidePrompt();

        dialoguePanel.Play(endingDialogue, OnEndingComplete);
    }
    private void OnEndingComplete()
    {
        isPlayingEnding = false;

        if (isPlayerInRange)
            ShowPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = true;

        if (!isPlayingEnding)
            ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = false;
        HidePrompt();
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<SimplePlayerMovement>() != null;
    }

    private void ShowPrompt()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        promptController?.Show(this, interactPrompt);
    }

    private void HidePrompt()
    {
        promptController?.Hide(this);
    }
}

