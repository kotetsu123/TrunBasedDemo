using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class FieldChestController : MonoBehaviour
{
    [SerializeField] private string chestId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string interactPrompt = "Press E";
    [SerializeField] private FieldInteractionPromptController promptController;
    [SerializeField] private List<InitialItemStack> rewards = new List<InitialItemStack>();
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;

    [Header("Lid Animation")]
    [SerializeField] private Transform lidTransform;
    [SerializeField] private Vector3 closedLidEuler = Vector3.zero;
    [SerializeField] private Vector3 openedLidEuler = new Vector3(-90f, 0f, 0f);
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [Header("Collision")]
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private bool disableColliderAfterOpen = false;

    private bool isPlayerInRange;
    private bool isOpened;
    private Collider cachedCollider;
    private Tween lidTween;

    public string ChestId => chestId;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();

        if (blockingCollider == null)
            blockingCollider = FindBlockingCollider();
    }

    private void Start()
    {
        if (promptController == null)
            promptController = FieldInteractionPromptController.Current;

        if (string.IsNullOrWhiteSpace(chestId))
        {
            chestId = gameObject.name;
            Debug.LogWarning($"[FieldChest] Chest id is empty. Use GameObject name instead. chestId={chestId}");
        }

        ApplyOpenedState(FieldBattleContext.IsChestOpened(chestId));
    }

    private void OnDestroy()
    {
        lidTween?.Kill();
    }

    private void Update()
    {
        if (isOpened || !isPlayerInRange)
            return;

        // First version interaction: stand inside the trigger and press E.
        if (Input.GetKeyDown(interactKey))
            TryOpen();
    }

    public void Configure(string newChestId)
    {
        if (!string.IsNullOrWhiteSpace(newChestId))
            chestId = newChestId;
    }

    public void TryOpen()
    {
        if (isOpened)
            return;

        if (string.IsNullOrWhiteSpace(chestId))
            chestId = gameObject.name;

        if (FieldBattleContext.IsChestOpened(chestId))
        {
            ApplyOpenedState(true);
            return;
        }

        GiveRewards();
        FieldBattleContext.MarkChestOpened(chestId);
        ApplyOpenedState(true);
        HidePrompt();

        Debug.Log($"[FieldChest] Opened chest: {chestId}");
    }

    private void GiveRewards()
    {
        foreach (InitialItemStack reward in rewards)
        {
            if (reward == null || reward.item == null || reward.count <= 0)
                continue;

            InventoryRuntimeState.AddItem(reward.item, reward.count);
            Debug.Log($"[FieldChest] Reward added. chestId={chestId}, item={reward.item.itemName}, count={reward.count}");
        }
    }

    private void ApplyOpenedState(bool opened)
    {
        isOpened = opened;

        if (closedVisual != null)
            closedVisual.SetActive(!isOpened);

        if (openedVisual != null)
            openedVisual.SetActive(isOpened);

        ApplyLidState(isOpened, Application.isPlaying);

        if (blockingCollider != null)
            blockingCollider.enabled = !isOpened;

        if (disableColliderAfterOpen && cachedCollider != null)
            cachedCollider.enabled = !isOpened;
    }

    private Collider FindBlockingCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col == null || col.isTrigger)
                continue;

            return col;
        }

        return null;
    }

    private void ApplyLidState(bool opened, bool animate)
    {
        if (lidTransform == null)
            return;

        lidTween?.Kill();

        Vector3 targetEuler = opened ? openedLidEuler : closedLidEuler;

        if (!animate || openDuration <= 0f)
        {
            lidTransform.localEulerAngles = targetEuler;
            return;
        }

        lidTween = lidTransform
            .DOLocalRotate(targetEuler, openDuration)
            .SetEase(openEase)
            .SetLink(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        isPlayerInRange = true;

        if (!isOpened)
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
