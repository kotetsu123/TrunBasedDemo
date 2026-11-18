using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDemoDebugController : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private CanvasGroup controlsGroup;

    [Header("Battle Demo")]
    [SerializeField] private Transform player;
    [SerializeField] private EncounterData bossEncounter;
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private SceneTransitionController transitionController;

    [Header("Story Preview")]
    [SerializeField] private DialogueData endingDialogue;
    [SerializeField] private VNDialoguePanelController dialoguePanel;
    [SerializeField] private DemoEndController demoEnd;

    private bool startingBattle;
    private bool playingStory;
    private Coroutine pendingStory;

    public bool IsBusy => startingBattle || playingStory;

    private bool CanStart => isActiveAndEnabled && !IsBusy && !FieldPauseState.IsPaused;

    private void OnEnable()
    {
        RefreshControls();
    }

    private void LateUpdate()
    {
        RefreshControls();
    }

    private void OnDisable()
    {
        if (pendingStory != null)
        {
            StopCoroutine(pendingStory);
            pendingStory = null;
            playingStory = false;
            FieldPauseState.SetPaused(false);
        }
        RefreshControls();
    }

    public void StartBattleDemo()
    {
        if (!CanStart)
            return;

        if (player == null || bossEncounter == null || !bossEncounter.ValidateConfig()
            || !Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            Debug.LogWarning("[BossDemo] Battle demo needs a player, a valid encounter and a built battle scene.");
            return;
        }

        bool hasLivingPlayer = false;
        foreach (Character member in PartyRuntimeState.PartyMembers)
        {
            if (member != null && member.Team == Team.Player && !member.isDead && member.Hp > 0)
            {
                hasLivingPlayer = true;
                break;
            }
        }
        if (!hasLivingPlayer)
        {
            Debug.LogWarning("[BossDemo] The battle demo needs at least one living party member.");
            return;
        }

        // Empty spawn IDs keep demo wins from clearing the real boss or starting its ending event.
        FieldBattleContext.SaveFieldReturnData(
            gameObject.scene.name, player.position, player.rotation,
            new string[0], new[] { bossEncounter.EncounterId });

        startingBattle = true;
        FieldPauseState.SetPaused(true);
        if (player.TryGetComponent(out Rigidbody body))
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        RefreshControls();

        if (transitionController != null && transitionController.isActiveAndEnabled)
            transitionController.StartBattleTransition(battleSceneName);
        else
            SceneManager.LoadScene(battleSceneName);
    }

    public void PlayStoryDemo()
    {
        if (!CanStart)
            return;

        if (endingDialogue == null || endingDialogue.Lines == null || endingDialogue.Lines.Count == 0
            || dialoguePanel == null || !dialoguePanel.isActiveAndEnabled
            || demoEnd == null || !demoEnd.isActiveAndEnabled)
        {
            Debug.LogWarning("[BossDemo] Story preview needs a non-empty dialogue, VN panel and demo ending.");
            return;
        }

        playingStory = true;
        FieldPauseState.SetPaused(true);
        RefreshControls();
        pendingStory = StartCoroutine(OpenStoryNextFrame());
    }

    private IEnumerator OpenStoryNextFrame()
    {
        // Do not let the button's opening click also advance the first VN line.
        yield return null;
        pendingStory = null;
        if (dialoguePanel == null || demoEnd == null)
        {
            playingStory = false;
            FieldPauseState.SetPaused(false);
            yield break;
        }
        dialoguePanel.Play(endingDialogue, HandleStoryCompleted);
    }

    private void HandleStoryCompleted()
    {
        playingStory = false;
        if (demoEnd != null)
            demoEnd.ShowDemoEnd();
        else
            FieldPauseState.SetPaused(false);
        RefreshControls();
    }

    private void RefreshControls()
    {
        if (controlsGroup == null)
            return;
        bool available = CanStart;
        controlsGroup.alpha = available ? 1f : 0f;
        controlsGroup.interactable = available;
        controlsGroup.blocksRaycasts = available;
    }
}
