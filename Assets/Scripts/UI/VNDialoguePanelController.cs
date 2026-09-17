using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VNDialoguePanelController : BasePanel
{
    [Header("Text")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.Space;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private DialogueData currentDialogue;
    private Action onComplete;
    private int currentLineIndex;

    public static VNDialoguePanelController Current { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Current = this;
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        // VN first version keeps the same input rhythm as the normal dialogue panel.
        if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            Advance();

        if (Input.GetKeyDown(closeKey))
            CompleteDialogue();
    }

    public void Play(DialogueData dialogueData, Action completeCallback = null)
    {
        if (dialogueData == null || dialogueData.Lines == null || dialogueData.Lines.Count == 0)
        {
            Debug.LogWarning("[VNDialoguePanel] Dialogue data is empty.");
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
