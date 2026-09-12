using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoEndController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private BasePanel endPanel;

    [Header("Buttons")]
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Button closeButton;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private void Awake()
    {
        if (endPanel == null)
            endPanel = GetComponent<BasePanel>();

        endPanel?.HideImmediate();

        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(BackToTitle);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDemoEnd);
    }

    private void OnDestroy()
    {
        if (backToTitleButton != null)
            backToTitleButton.onClick.RemoveListener(BackToTitle);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseDemoEnd);
    }

    public void ShowDemoEnd()
    {
        // DialoguePanel 播完时会解除 FieldPauseState，所以 EndPanel 打开时要重新暂停 Field。
        FieldPauseState.SetPaused(true);
        endPanel?.Show();
    }

    public void CloseDemoEnd()
    {
        endPanel?.Hide();
        FieldPauseState.SetPaused(false);
    }

    public void BackToTitle()
    {
        FieldPauseState.SetPaused(false);
        SceneManager.LoadScene(titleSceneName);
    }
}
