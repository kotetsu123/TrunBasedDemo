using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [SerializeField] private string speakerId;
    [SerializeField] private string speakerName;
    [SerializeField] private Sprite portrait;
    [TextArea(2, 4)]
    [SerializeField] private string text;

    public string SpeakerId => speakerId;
    public string SpeakerName => speakerName;
    public Sprite Portrait => portrait;
    public string Text => text;
}

[CreateAssetMenu(fileName = "DialogueData_", menuName = "Game Data/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string dialogueId;
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public string DialogueId => dialogueId;
    public IReadOnlyList<DialogueLine> Lines => lines;
}
