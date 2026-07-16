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
