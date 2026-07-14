using System;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialPanelController : BasePanel
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipConfirmRoot;
    [SerializeField] private Button confirmSkipButton;
    [SerializeField] private Button cancelSkipButton;
    [SerializeField] private KeyCode nextKey = KeyCode.Space;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    private TutorialData currentTutorial;
    private Action onComplete;
    private int currentStepIndex;
    private bool isSkipConfirmOpen;

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

        if (confirmSkipButton != null)
            confirmSkipButton.onClick.AddListener(ConfirmSkip);

        if (cancelSkipButton != null)
            cancelSkipButton.onClick.AddListener(CancelSkip);

        HideSkipConfirm();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        if (nextButton != null)
            nextButton.onClick.RemoveListener(Next);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(Skip);

        if (confirmSkipButton != null)
            confirmSkipButton.onClick.RemoveListener(ConfirmSkip);

        if (cancelSkipButton != null)
            cancelSkipButton.onClick.RemoveListener(CancelSkip);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (isSkipConfirmOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                ConfirmSkip();

            if (Input.GetKeyDown(skipKey))
                CancelSkip();

            return;
        }

        if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.Return))
            Next();

        if (Input.GetMouseButtonDown(0) && !IsPointerOverButton())
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
        HideSkipConfirm();

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

        if (skipConfirmRoot == null)
        {
            Debug.LogWarning("[TutorialPanel] Skip confirm root is missing. Skip immediately.");
            CompleteTutorial();
            return;
        }

        isSkipConfirmOpen = true;
        skipConfirmRoot.SetActive(true);
    }

    public void ConfirmSkip()
    {
        if (currentTutorial == null)
            return;

        CompleteTutorial();
    }

    public void CancelSkip()
    {
        HideSkipConfirm();
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
        HideSkipConfirm();

        Hide();
        FieldPauseState.SetPaused(false);
        completeCallback?.Invoke();
    }

    private void HideSkipConfirm()
    {
        isSkipConfirmOpen = false;

        if (skipConfirmRoot != null)
            skipConfirmRoot.SetActive(false);
    }

    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.GetComponentInParent<Button>() != null)
                return true;
        }

        return false;
    }
}
