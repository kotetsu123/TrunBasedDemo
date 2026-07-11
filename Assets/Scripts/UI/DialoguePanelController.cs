using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanelController : BasePanel
{
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private KeyCode nextKey = KeyCode.Space;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private DialogueData currentDialogue;
    private Action onComplete;
    private int currentLineIndex;

    public static DialoguePanelController Current { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Current = this;
        HideImmediate();

        if (nextButton != null)
            nextButton.onClick.AddListener(Advance);
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;

        if (nextButton != null)
            nextButton.onClick.RemoveListener(Advance);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        // First version only advances or completes the current dialogue.
        if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            Advance();

        if (Input.GetKeyDown(closeKey))
            CompleteDialogue();
    }

    public void Play(DialogueData dialogueData, Action completeCallback = null)
    {
        if (dialogueData == null || dialogueData.Lines == null || dialogueData.Lines.Count == 0)
        {
            Debug.LogWarning("[DialoguePanel] Dialogue data is empty.");
            completeCallback?.Invoke();
            return;
        }

        currentDialogue = dialogueData;
        onComplete = completeCallback;
        currentLineIndex = 0;

        FieldPauseState.SetPaused(true);
        Show();
        RefreshLine();
    }

    public void Advance()
    {
        if (currentDialogue == null)
            return;

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.Lines.Count)
        {
            CompleteDialogue();
            return;
        }

        RefreshLine();
    }

    private void RefreshLine()
    {
        DialogueLine line = currentDialogue.Lines[currentLineIndex];
        if (line == null)
            return;

        if (speakerNameText != null)
            speakerNameText.text = line.SpeakerName;

        if (dialogueText != null)
            dialogueText.text = line.Text;

        if (portraitImage != null)
        {
            portraitImage.sprite = line.Portrait;
            portraitImage.enabled = line.Portrait != null;
        }
    }

    private void CompleteDialogue()
    {
        Action completeCallback = onComplete;

        currentDialogue = null;
        onComplete = null;
        currentLineIndex = 0;

        Hide();
        FieldPauseState.SetPaused(false);
        completeCallback?.Invoke();
    }
}
