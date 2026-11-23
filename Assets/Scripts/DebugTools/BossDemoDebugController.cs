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

    [Header("Story Retreat")]
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
        // Consume on scene entry, before FieldCreator clears the battle return data in Start.
        if (FieldBattleContext.TryConsumeBossStoryRetreat(gameObject.scene.name))
        {
            playingStory = true;
            pendingStory = StartCoroutine(OpenStoryNextFrame());
        }
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
        StartDemo(bossStoryBattle: false);
    }

    public void PlayStoryDemo()
    {
        StartDemo(bossStoryBattle: true);
    }

    private void StartDemo(bool bossStoryBattle)
    {
        if (!CanStart)
            return;

        if (player == null || bossEncounter == null || !bossEncounter.ValidateConfig()
            || !Application.CanStreamedLevelBeLoaded(battleSceneName))
        {
            Debug.LogWarning("[BossDemo] Battle demo needs a player, a valid encounter and a built battle scene.");
            return;
        }

        if (bossStoryBattle && !CanPlayEnding())
        {
            Debug.LogWarning("[BossDemo] Story battle needs a non-empty dialogue, VN panel and demo ending.");
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
            System.Array.Empty<string>(), new[] { bossEncounter.EncounterId }, bossStoryBattle);

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

    private bool CanPlayEnding()
    {
        return endingDialogue != null && endingDialogue.Lines != null && endingDialogue.Lines.Count > 0
            && dialoguePanel != null && dialoguePanel.isActiveAndEnabled && demoEnd != null && demoEnd.isActiveAndEnabled;
    }

    private IEnumerator OpenStoryNextFrame()
    {
        // Let the returning field finish initializing before the VN takes over.
        yield return null;
        pendingStory = null;
        if (!CanPlayEnding())
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
