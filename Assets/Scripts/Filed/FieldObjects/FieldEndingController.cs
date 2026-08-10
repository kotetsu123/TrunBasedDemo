using UnityEngine;

public class FieldEndingController : MonoBehaviour
{
    [SerializeField] private DialogueData endingDialogue;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Finish";
    [SerializeField] private bool disableAfterComplete = true;
    [SerializeField] private bool hideVisualAfterComplete = true;
    [SerializeField] private bool disableColliderAfterComplete = true;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private DialoguePanelController dialoguePanel;

    private bool isPlayerInRange;
    private bool isPlayingEnding;
    private bool isCompleted;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();
    }

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
            ApplyCompletedState();
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

    private void ApplyCompletedState()
    {
        HidePrompt();

        if (hideVisualAfterComplete && visualRoot != null)
            visualRoot.SetActive(false);

        if (disableColliderAfterComplete && triggerCollider != null)
            triggerCollider.enabled = false;
    }
}

