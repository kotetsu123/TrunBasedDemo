using UnityEngine;

public class FieldTutorialController : MonoBehaviour
{
    [SerializeField] private TutorialData tutorialData;
    [SerializeField] private TutorialPanelController tutorialPanel;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelaySeconds = 0.25f;

    private bool hasPlayed;

    private void Start()
    {
        if (!playOnStart)
            return;

        // Field 场景从 Battle 返回时也会重新 Start。
        // 这时不应该自动弹教程，否则 TutorialPanel 会把 FieldPauseState 设成暂停。
        if (FieldBattleContext.HasFieldReturnData || FieldBattleContext.IsEncounterCooldownActive)
            return;

        if (startDelaySeconds > 0f)
            Invoke(nameof(PlayTutorial), startDelaySeconds);
        else
            PlayTutorial();
    }

    public void PlayTutorial()
    {
        if (hasPlayed)
            return;

        if (tutorialPanel == null)
            tutorialPanel = TutorialPanelController.Current;

        if (tutorialPanel == null)
        {
            Debug.LogWarning("[FieldTutorial] TutorialPanelController is missing.");
            return;
        }

        if (tutorialData == null)
        {
            Debug.LogWarning("[FieldTutorial] TutorialData is missing.");
            return;
        }

        if (TutorialRuntimeState.IsCompleted(tutorialData.TutorialId))
        {
            hasPlayed = true;
            Debug.Log($"[FieldTutorial] Tutorial already completed. tutorialId={tutorialData.TutorialId}");
            return;
        }

        hasPlayed = true;
        tutorialPanel.Play(tutorialData, OnTutorialComplete);
    }

    private void OnTutorialComplete()
    {
        if (tutorialData == null)
            return;

        TutorialRuntimeState.MarkCompleted(tutorialData.TutorialId);
    }
}
