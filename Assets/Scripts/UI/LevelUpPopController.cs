using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelUpPopController : BasePanel
{
    [SerializeField] private TMP_Text nameTxt;
    [SerializeField] private TMP_Text levelTxt;
    [SerializeField] private TMP_Text titleTxt;
    [SerializeField] private float visibleDuration=1.0f;

    private Coroutine _playRoutine;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
    }
    public void Play(LevelUpResult result)
    {
        if (result == null || !result.DidLevelUp)
            return;

        if(_playRoutine!=null)
            StopCoroutine( _playRoutine );

        _playRoutine=StartCoroutine(PlayRoutine(result));
    }
    private IEnumerator PlayRoutine(LevelUpResult result)
    {
        if (nameTxt != null)
            nameTxt.text = result.characterName;
        if (titleTxt != null)
            titleTxt.text = "Level Up!";
        if (levelTxt != null)
            levelTxt.text = $"Lv.{result.beforeLevel} ¡ú  Lv.{result.afterLevel}";
        base.Show();
        yield return new WaitForSeconds(fadeDuration + visibleDuration);

        base.Hide();
        yield return  new WaitForSeconds(fadeDuration);

        _playRoutine = null;
    }

    public float GetTotalDuration()
    {
        return fadeDuration + visibleDuration + fadeDuration;
    }
}
