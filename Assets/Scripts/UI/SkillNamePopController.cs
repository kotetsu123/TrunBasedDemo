using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


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
  public void Play(string skillName)
    {
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(PlayRoutine(skillName));
    }
    private IEnumerator PlayRoutine(string skillName)
    {
        skillNameText.text = skillName;

        base.Show();

        yield return new WaitForSeconds(visibleDuration+fadeDuration);

        base.Hide();
        yield return new WaitForSeconds(fadeDuration);

        _showRoutine = null;

    }
   
}
