using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FieldAutoDialogueEventController : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private string eventId;
    [SerializeField] private string requiredClearedSpawnId;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private float playDelaySeconds = 0.5f;

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialoguePanelController dialoguePanel;
    [SerializeField] private bool useVnDialoguePanel;
    [SerializeField] private VNDialoguePanelController vnDialoguePanel;

    [Header("Completion")]
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool isPlaying;

    private void Start()
    {
        TryStartAutoEvent();
    }

    private void TryStartAutoEvent()
    {
        if (isPlaying)
        {
            Debug.Log($"[FieldAutoDialogueEvent] Skip because event is already playing. eventId={eventId}");
            return;
        }

        // eventId 是这个自动剧情的唯一记录 ID，用来避免同一轮运行里重复播放。
        if (playOnce && FieldAutoEventRuntimeState.IsCompleted(eventId))
        {
            Debug.Log($"[FieldAutoDialogueEvent] Skip completed event. eventId={eventId}");
            return;
        }

        // Boss 战后会把 boss_spawn_001 记录到 FieldBattleContext 的 cleared spawn 里。
        // 这里用它作为条件，避免玩家没打 Boss 就直接触发结尾剧情。
        if (!string.IsNullOrWhiteSpace(requiredClearedSpawnId) &&
            !FieldBattleContext.IsSpawnCleard(requiredClearedSpawnId))
        {
            Debug.Log($"[FieldAutoDialogueEvent] Waiting for cleared spawn. eventId={eventId}, requiredClearedSpawnId={requiredClearedSpawnId}");
            return;
        }

        if (dialogueData == null)
        {
            Debug.LogWarning($"[FieldAutoDialogueEvent] DialogueData is missing. eventId={eventId}");
            return;
        }

        StartCoroutine(PlayEventRoutine());
    }

    private IEnumerator PlayEventRoutine()
    {
        isPlaying = true;
        Debug.Log($"[FieldAutoDialogueEvent] Start auto dialogue. eventId={eventId}, dialogueId={dialogueData.DialogueId}");

        if (playDelaySeconds > 0f)
            yield return new WaitForSeconds(playDelaySeconds);

        if (useVnDialoguePanel)
        {
            PlayVnDialogue();
            yield break;
        }

        if (dialoguePanel == null)
            dialoguePanel = DialoguePanelController.Current;

        if (dialoguePanel == null)
        {
            Debug.LogWarning($"[FieldAutoDialogueEvent] DialoguePanelController is missing. eventId={eventId}");
            isPlaying = false;
            yield break;
        }

        dialoguePanel.Play(dialogueData, OnDialogueComplete);
    }

    private void PlayVnDialogue()
    {
        if (vnDialoguePanel == null)
            vnDialoguePanel = VNDialoguePanelController.Current;

        if (vnDialoguePanel == null)
        {
            Debug.LogWarning($"[FieldAutoDialogueEvent] VNDialoguePanelController is missing. eventId={eventId}");
            isPlaying = false;
            return;
        }

        vnDialoguePanel.Play(dialogueData, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        isPlaying = false;
        Debug.Log($"[FieldAutoDialogueEvent] Complete auto dialogue. eventId={eventId}");

        if (playOnce)
            FieldAutoEventRuntimeState.MarkCompleted(eventId);

        // 自动剧情播完后，把后续流程交给 Inspector 配置。
        // 例如 Boss 结尾剧情可以在这里打开 DemoEndPanel。
        onDialogueFinished?.Invoke();
    }
}
