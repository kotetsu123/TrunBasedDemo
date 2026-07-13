using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelController : BasePanel
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private KeyCode nextKey = KeyCode.Space;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    private TutorialData currentTutorial;
    private Action onComplete;
    private int currentStepIndex;

    public static TutorialPanelController Current { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Current = this;
        HideImmediate();

        if (nextButton != null)
            nextButton.onClick.AddListener(Next);

        if (skipButton != null)
            skipButton.onClick.AddListener(Skip);
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        if (nextButton != null)
            nextButton.onClick.RemoveListener(Next);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(Skip);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.Return))
            Next();

        if (Input.GetKeyDown(skipKey))
            Skip();
    }

    public void Play(TutorialData tutorialData, Action completeCallback = null)
    {
        if (tutorialData == null || tutorialData.Steps == null || tutorialData.Steps.Count == 0)
        {
            Debug.LogWarning("[TutorialPanel] Tutorial data is empty.");
            completeCallback?.Invoke();
            return;
        }

        currentTutorial = tutorialData;
        onComplete = completeCallback;
        currentStepIndex = 0;

        FieldPauseState.SetPaused(true);
        Show();
        RefreshStep();
    }

    public void Next()
    {
        if (currentTutorial == null)
            return;

        currentStepIndex++;

        if (currentStepIndex >= currentTutorial.Steps.Count)
        {
            CompleteTutorial();
            return;
        }

        RefreshStep();
    }

    public void Skip()
    {
        if (currentTutorial == null)
            return;

        CompleteTutorial();
    }

    private void RefreshStep()
    {
        TutorialStep step = currentTutorial.Steps[currentStepIndex];
        if (step == null)
            return;

        if (titleText != null)
            titleText.text = step.Title;

        if (messageText != null)
            messageText.text = step.Message;
    }

    private void CompleteTutorial()
    {
        Action completeCallback = onComplete;

        currentTutorial = null;
        onComplete = null;
        currentStepIndex = 0;

        Hide();
        FieldPauseState.SetPaused(false);
        completeCallback?.Invoke();
    }
}
