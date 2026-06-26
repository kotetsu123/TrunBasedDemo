using System.Collections.Generic;
using UnityEngine;

public class FieldChestController : MonoBehaviour
{
    [SerializeField] private string chestId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private List<InitialItemStack> rewards = new List<InitialItemStack>();
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;
    [SerializeField] private bool disableColliderAfterOpen = false;

    private bool isPlayerInRange;
    private bool isOpened;
    private Collider cachedCollider;

    public string ChestId => chestId;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(chestId))
        {
            chestId = gameObject.name;
            Debug.LogWarning($"[FieldChest] Chest id is empty. Use GameObject name instead. chestId={chestId}");
        }

        ApplyOpenedState(FieldBattleContext.IsChestOpened(chestId));
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

        if (disableColliderAfterOpen && cachedCollider != null)
            cachedCollider.enabled = !isOpened;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            isPlayerInRange = false;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<SimplePlayerMovement>() != null;
    }
}
