using System.Collections;
using UnityEngine;

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

    private bool isPlaying;

    private void Start()
    {
        TryStartAutoEvent();
    }

    private void TryStartAutoEvent()
    {
        if (isPlaying)
            return;

        // eventId 是这个自动剧情的唯一记录 ID，用来避免同一轮运行里重复播放。
        if (playOnce && FieldAutoEventRuntimeState.IsCompleted(eventId))
            return;

        // Boss 战后会把 boss_spawn_001 记录到 FieldBattleContext 的 cleared spawn 里。
        // 这里用它作为条件，避免玩家没打 Boss 就直接触发结尾剧情。
        if (!string.IsNullOrWhiteSpace(requiredClearedSpawnId) &&
            !FieldBattleContext.IsSpawnCleard(requiredClearedSpawnId))
            return;

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

        if (playDelaySeconds > 0f)
            yield return new WaitForSeconds(playDelaySeconds);

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

    private void OnDialogueComplete()
    {
        isPlaying = false;

        if (playOnce)
            FieldAutoEventRuntimeState.MarkCompleted(eventId);
    }
}
