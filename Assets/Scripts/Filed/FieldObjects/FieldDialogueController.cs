using UnityEngine;

public class FieldDialogueController : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Talk";
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private DialoguePanelController dialoguePanel;

    private bool isPlayerInRange;
    private bool isPlayingDialogue;

    private void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;
    }

    private void Update()
    {
        if (!isPlayerInRange || isPlayingDialogue)
            return;

        if (Input.GetKeyDown(interactKey))
            StartDialogue();
    }

    public void StartDialogue()
    {
        if (isPlayingDialogue)
            return;

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;

        if (dialoguePanel == null)
        {
            Debug.LogWarning($"[FieldDialogue] DialoguePanelController is missing. object={gameObject.name}");
            return;
        }

        if (dialogueData == null)
        {
            Debug.LogWarning($"[FieldDialogue] DialogueData is missing. object={gameObject.name}");
            return;
        }

        isPlayingDialogue = true;
        HidePrompt();
        dialoguePanel.Play(dialogueData, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        isPlayingDialogue = false;

        if (isPlayerInRange)
            ShowPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = true;

        if (!isPlayingDialogue)
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
