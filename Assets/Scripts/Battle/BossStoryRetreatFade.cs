using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BossStoryRetreatFade : MonoBehaviour
{
    private const float FadeToBlackSeconds = .95f;
    private const float RevealSeconds = .65f;
    private const float EndingReadyTimeoutSeconds = 2f;
    private CanvasGroup overlay;
    private VNDialoguePanelController ending;

    public static void Begin(string sceneName)
    {
        var root = new GameObject("Boss Story Fade", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(BossStoryRetreatFade));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var fill = new GameObject("Black", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)fill.transform;
        rect.SetParent(root.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = Color.black;

        var fade = root.GetComponent<BossStoryRetreatFade>();
        fade.overlay = root.GetComponent<CanvasGroup>();
        fade.overlay.alpha = 0f;
        DontDestroyOnLoad(root);
        fade.StartCoroutine(fade.Play(sceneName));
    }

    private IEnumerator Play(string sceneName)
    {
        yield return Fade(0f, 1f, FadeToBlackSeconds);
        // Present a fully black frame before starting the scene swap.
        yield return null;

        FieldBattleContext.QueueBossStoryRetreat();
        yield return SceneManager.LoadSceneAsync(sceneName);

        ending = VNDialoguePanelController.Current;
        if (ending != null)
        {
            ending.TransitionInputBlocked = true;
            float deadline = Time.unscaledTime + EndingReadyTimeoutSeconds;
            while (ending != null && !ending.IsOpen && Time.unscaledTime < deadline)
                yield return null;
        }

        yield return Fade(1f, 0f, RevealSeconds);
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            overlay.alpha = Mathf.SmoothStep(from, to, elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        overlay.alpha = to;
    }

    private void OnDestroy()
    {
        if (ending != null)
            ending.TransitionInputBlocked = false;
    }
}
