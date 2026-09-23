using System.Collections.Generic;
using UnityEngine;

public enum DialoguePortraitSide
{
    Left,
    Right
}

[System.Serializable]
public class DialogueLine
{
    [SerializeField] private string speakerId;
    [SerializeField] private string speakerName;
    [SerializeField] private Sprite portrait;
    [SerializeField] private DialoguePortraitSide portraitSide;
    [TextArea(2, 4)]
    [SerializeField] private string text;

    public string SpeakerId => speakerId;
    public string SpeakerName => speakerName;
    public Sprite Portrait => portrait;
    public DialoguePortraitSide PortraitSide => portraitSide;
    public string Text => text;
}

[CreateAssetMenu(fileName = "DialogueData_", menuName = "Game Data/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string dialogueId;
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Presentation")]
    [SerializeField] private Sprite background;
    [SerializeField] private bool useTypewriter;
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 40f;

    public string DialogueId => dialogueId;
    public IReadOnlyList<DialogueLine> Lines => lines;
    public Sprite Background => background;
    public bool UseTypewriter => useTypewriter;
    public float CharactersPerSecond => charactersPerSecond;
}
