using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float waitBeforLoad = 0.2f;
    [SerializeField] private bool pauseFieldDuringTransition = true;

    private bool isTransitioning;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
    public void StartBattleTransition(string sceneName)
    {
        StartSceneTransition(sceneName);
    }

    public void StartSceneTransition(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName));
    }
    IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        // 进入战斗的 fade 期间 Field 场景还没有切走。
        // 这里先暂停 Field，避免敌人继续追击、玩家继续移动，导致画面上还在碰撞/推进。
        if (pauseFieldDuringTransition)
            FieldPauseState.SetPaused(true);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }
        yield return new WaitForSeconds(waitBeforLoad);

        SceneManager.LoadScene(sceneName);
    }
}
