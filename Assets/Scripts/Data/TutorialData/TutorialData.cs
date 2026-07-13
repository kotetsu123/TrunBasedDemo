using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [SerializeField] private string title;
    [TextArea(2, 5)]
    [SerializeField] private string message;

    public string Title => title;
    public string Message => message;
}

[CreateAssetMenu(fileName = "TutorialData_", menuName = "Game Data/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [SerializeField] private string tutorialId;
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    public string TutorialId => tutorialId;
    public IReadOnlyList<TutorialStep> Steps => steps;
}
