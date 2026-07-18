using UnityEngine;

public class BattleTutorialController : MonoBehaviour
{
    [SerializeField] private TutorialData tutorialData;
    [SerializeField] private TutorialPanelController tutorialPanel;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0.5f;

    private bool hasPlayed;

    private void Start()
    {
        if (!playOnStart)
            return;
        if (startDelay > 0f)
            Invoke(nameof(PlayTutorial), startDelay);
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
            Debug.LogWarning("[BattleTutorial] TutorialPanelController is missing.");
            return;
        }

        if (tutorialData == null)
        {
            Debug.LogWarning("[BattleTutorial] TutorialData is missing.");
            return;
        }

        if (TutorialRuntimeState.IsCompleted(tutorialData.TutorialId))
        {
            hasPlayed = true;
            Debug.Log($"[BattleTutorial] Tutorial already completed. tutorialId={tutorialData.TutorialId}");
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
