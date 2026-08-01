using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


// This component is still named SkillNamePopController to keep existing Unity bindings safe.
// It now works as the shared battle popup panel for skill names and battle event messages.
public class SkillNamePopController : BasePanel
{   
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private float visibleDuration = 0.8f;

    private Coroutine _showRoutine;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
    }
  public void Play(string message)
    {
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(PlayRoutine(message));
    }
    private IEnumerator PlayRoutine(string message)
    {
        skillNameText.text = message;

        base.Show();

        yield return new WaitForSeconds(visibleDuration+fadeDuration);

        base.Hide();
        yield return new WaitForSeconds(fadeDuration);

        _showRoutine = null;

    }
   
}

