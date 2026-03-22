using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class SkillNamePopController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float visibleDuration = 0.8f;

    private Coroutine _showRoutine;

    private void Awake()
    {
        HideImmediate();
    }
  public void Show(string skillName)
    {
        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowRoutine(skillName));
    }
    private IEnumerator ShowRoutine(string skillName)
    {
        skillNameText.text = skillName;
        canvasGroup.alpha = 1f;

        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(visibleDuration);
        yield return Fade(1f, 0f, fadeDuration);

        canvasGroup.alpha = 0f;
        _showRoutine = null;

    }
    private IEnumerator Fade(float from,float to,float duration)
    {
        float time = 0f;
        canvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
    private void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
