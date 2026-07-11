using UnityEngine;

public class FieldRecruitController : MonoBehaviour
{
    [SerializeField] private string recruitId;
    [SerializeField] private string characterId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Recruit";
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private DialoguePanelController dialoguePanel;
    [SerializeField] private DialogueData preRecruitDialogue;
    [SerializeField] private FieldPartyHudController partyHudController;
    [SerializeField] private bool disableAfterRecruit = true;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider interactionCollider;

    private bool isPlayerInRange;
    private bool isRecruited;
    private bool isPlayingRecruitDialogue;
    private bool hasPlayedPreRecruitDialogue;
    private Renderer[] cachedRenderers;

    private void Awake()
    {
        CacheReferences();
    }

    public void Configure(
        FieldRecruitPointEntry entry,
        CharacterDataBase newCharacterDataBase,
        FieldPartyHudController newPartyHudController,
        GameObject newVisualRoot)
    {
        if (entry == null)
            return;

        recruitId = entry.RecruitId;
        characterId = entry.CharacterId;
        interactPrompt = entry.InteractPrompt;
        preRecruitDialogue = entry.PreRecruitDialogue;
        disableAfterRecruit = entry.DisableAfterRecruit;
        characterDataBase = newCharacterDataBase;
        partyHudController = newPartyHudController;

        if (newVisualRoot != null)
            visualRoot = newVisualRoot;

        if (string.IsNullOrWhiteSpace(recruitId))
            recruitId = gameObject.name;

        CacheReferences();
        RefreshRecruitState();
    }

    private void CacheReferences()
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider>();

        cachedRenderers = visualRoot != null
            ? visualRoot.GetComponentsInChildren<Renderer>(true)
            : GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if (partyHudController == null)
            partyHudController = FindObjectOfType<FieldPartyHudController>();

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;

        if (string.IsNullOrWhiteSpace(recruitId))
            recruitId = gameObject.name;

        RefreshRecruitState();
    }

    public static void RefreshAllRecruitStates()
    {
        FieldRecruitController[] controllers = FindObjectsOfType<FieldRecruitController>(true);
        foreach (FieldRecruitController controller in controllers)
        {
            if (controller == null)
                continue;

            if (!controller.gameObject.activeSelf)
                controller.gameObject.SetActive(true);

            controller.RefreshRecruitState();
        }
    }

    public void RefreshRecruitState()
    {
        ApplyRecruitedState(PartyRuntimeState.HasMember(characterId));
    }

    private void Update()
    {
        if (isRecruited || isPlayingRecruitDialogue || !isPlayerInRange)
            return;

        if (Input.GetKeyDown(interactKey))
            StartRecruitInteraction();
    }

    private void StartRecruitInteraction()
    {
        if (preRecruitDialogue == null || hasPlayedPreRecruitDialogue)
        {
            TryRecruit();
            return;
        }

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;

        if (dialoguePanel == null)
        {
            Debug.LogWarning($"[FieldRecruit] DialoguePanelController is missing. Recruit will continue without dialogue. recruitId={recruitId}, characterId={characterId}");
            TryRecruit();
            return;
        }

        isPlayingRecruitDialogue = true;
        HidePrompt();
        dialoguePanel.Play(preRecruitDialogue, OnPreRecruitDialogueComplete);
    }

    private void OnPreRecruitDialogueComplete()
    {
        isPlayingRecruitDialogue = false;
        hasPlayedPreRecruitDialogue = true;

        TryRecruit();
    }

    public void TryRecruit()
    {
        if (isRecruited)
            return;

        if (characterDataBase == null)
        {
            Debug.LogWarning($"[FieldRecruit] CharacterDataBase is null. recruitId={recruitId}, characterId={characterId}");
            if (isPlayerInRange)
                ShowPrompt();
            return;
        }

        Character characterTemplate = characterDataBase.FindById(characterId);
        if (characterTemplate == null)
        {
            Debug.LogWarning($"[FieldRecruit] Character not found. recruitId={recruitId}, characterId={characterId}");
            if (isPlayerInRange)
                ShowPrompt();
            return;
        }

        if (!PartyRuntimeState.TryRecruitMember(characterTemplate, out Character recruitedMember))
        {
            ApplyRecruitedState(PartyRuntimeState.HasMember(characterId));
            return;
        }

        Debug.Log($"[FieldRecruit] {recruitedMember.Name} joined the party. recruitId={recruitId}, characterId={characterId}");

        partyHudController?.Refresh();
        HidePrompt();
        ApplyRecruitedState(true);
    }

    private void ApplyRecruitedState(bool recruited)
    {
        isRecruited = recruited;

        if (isRecruited)
        {
            isPlayerInRange = false;
            HidePrompt();
        }

        if (disableAfterRecruit && isRecruited)
            SetRecruitVisible(false);
        else
            SetRecruitVisible(true);
    }

    private void SetRecruitVisible(bool visible)
    {
        if (visualRoot != null && visualRoot != gameObject)
        {
            visualRoot.SetActive(visible);
            return;
        }

        // Fallback for temporary scene objects that do not have a dedicated visualRoot yet.
        // Once a RecruitPoint has a child model assigned as visualRoot, only that child is toggled.
        foreach (Renderer cachedRenderer in cachedRenderers)
        {
            if (cachedRenderer == null)
                continue;

            cachedRenderer.enabled = visible;
        }

        // Only disable the point collider in fallback mode. With visualRoot setup, the point stays alive for Load refresh.
        if (interactionCollider != null)
            interactionCollider.enabled = visible;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = true;

        if (!isRecruited)
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
