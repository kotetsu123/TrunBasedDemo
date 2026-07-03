using UnityEngine;

public class FieldRecruitController : MonoBehaviour
{
    [SerializeField] private string recruitId;
    [SerializeField] private string characterId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Recruit";
    [SerializeField] private CharacterDataBase characterDataBase;
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private FieldPartyHudController partyHudController;
    [SerializeField] private bool disableAfterRecruit = true;

    private bool isPlayerInRange;
    private bool isRecruited;

    private void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if (partyHudController == null)
            partyHudController = FindObjectOfType<FieldPartyHudController>();

        if (string.IsNullOrWhiteSpace(recruitId))
            recruitId = gameObject.name;

        isRecruited = PartyRuntimeState.HasMember(characterId);
        ApplyRecruitedState(isRecruited);
    }

    private void Update()
    {
        if (isRecruited || !isPlayerInRange)
            return;

        if (Input.GetKeyDown(interactKey))
            TryRecruit();
    }

    public void TryRecruit()
    {
        if (isRecruited)
            return;

        if (characterDataBase == null)
        {
            Debug.LogWarning($"[FieldRecruit] CharacterDataBase is null. recruitId={recruitId}, characterId={characterId}");
            return;
        }

        Character characterTemplate = characterDataBase.FindById(characterId);
        if (characterTemplate == null)
        {
            Debug.LogWarning($"[FieldRecruit] Character not found. recruitId={recruitId}, characterId={characterId}");
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
            HidePrompt();

        if (disableAfterRecruit && isRecruited)
            gameObject.SetActive(false);
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
