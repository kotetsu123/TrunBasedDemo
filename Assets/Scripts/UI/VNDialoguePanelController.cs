using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class VNDialoguePanelController : BasePanel
{
    [Header("Text")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Portrait")]
    [FormerlySerializedAs("portraitImage")]
    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private Image rightPortraitImage;
    [Range(0f, 1f)]
    [SerializeField] private float inactivePortraitAlpha = 0.45f;

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

        ResetPortraits();
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

        RefreshPortrait(line);
    }

    private void RefreshPortrait(DialogueLine line)
    {
        bool isLeftSpeaker = line.PortraitSide == DialoguePortraitSide.Left;
        Image activePortrait = isLeftSpeaker ? leftPortraitImage : rightPortraitImage;
        Image inactivePortrait = isLeftSpeaker ? rightPortraitImage : leftPortraitImage;

        // Each side keeps its last portrait, while the current speaker is highlighted.
        if (activePortrait != null && line.Portrait != null)
        {
            activePortrait.sprite = line.Portrait;
            activePortrait.enabled = true;
        }

        SetPortraitAlpha(activePortrait, 1f);
        SetPortraitAlpha(inactivePortrait, inactivePortraitAlpha);
    }

    private void ResetPortraits()
    {
        ResetPortrait(leftPortraitImage);
        ResetPortrait(rightPortraitImage);
    }

    private static void ResetPortrait(Image portrait)
    {
        if (portrait == null)
            return;

        portrait.sprite = null;
        portrait.enabled = false;
        SetPortraitAlpha(portrait, 1f);
    }

    private static void SetPortraitAlpha(Image portrait, float alpha)
    {
        if (portrait == null)
            return;

        Color color = portrait.color;
        color.a = alpha;
        portrait.color = color;
    }

    private void CompleteDialogue()
    {
        Action completeCallback = onComplete;

        currentDialogue = null;
        onComplete = null;
        currentLineIndex = 0;

        ResetPortraits();
        Hide();
        FieldPauseState.SetPaused(false);
        completeCallback?.Invoke();
    }
}
