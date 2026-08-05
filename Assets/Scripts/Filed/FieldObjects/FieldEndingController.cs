using UnityEngine;

public class FieldEndingController : MonoBehaviour
{
    [SerializeField] private DialogueData endingDialogue;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Finish";
    [SerializeField] private bool disableAfterComplete = true;
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private DialoguePanelController dialoguePanel;

    private bool isPlayerInRange;
    private bool isPlayingEnding;
    private bool isCompleted;

    private void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;
    }

    private void Update()
    {
        if (!isPlayerInRange || isPlayingEnding || isCompleted)
            return;

        if (Input.GetKeyDown(interactKey))
            StartEnding();
    }

    private void StartEnding()
    {
        if (isPlayingEnding || isCompleted)
            return;

        if (dialoguePanel == null)
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

        if (disableAfterComplete)
        {
            isCompleted = true;
            HidePrompt();
            return;
        }

        if (isPlayerInRange)
            ShowPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = true;

        if (!isPlayingEnding && !isCompleted)
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

