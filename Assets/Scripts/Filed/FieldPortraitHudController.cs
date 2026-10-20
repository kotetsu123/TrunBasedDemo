using UnityEngine;
using UnityEngine.UI;

public sealed class FieldPortraitHudController : MonoBehaviour
{
    [SerializeField] private CanvasGroup hudGroup;
    [SerializeField] private Button portraitButton;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image hpArc;
    [SerializeField] private Image mpArc;
    [SerializeField] private GameObject downMarker;
    [SerializeField] private FieldStatusPanelController statusPanel;
    [SerializeField, Range(.1f, .5f)] private float arcSpan = 160f / 360f;

    private Character member;
    private int shownHp, shownMaxHp, shownMp, shownMaxMp;
    private bool shownDead;

    private void Awake()
    {
        portraitButton.onClick.AddListener(OpenStatus);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unbind();
        hudGroup.alpha = 0;
        hudGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        portraitButton.onClick.RemoveListener(OpenStatus);
        Unbind();
    }

    private void LateUpdate()
    {
        var leader = FindLeader();
        if (!ReferenceEquals(leader, member)) Refresh();
        else if (member != null && (shownHp != member.Hp || shownMaxHp != member.MaxHp ||
            shownMp != member.Mp || shownMaxMp != member.MaxMp ||
            shownDead != (member.isDead || member.Hp <= 0) || portraitImage.sprite != member.Portrait))
            Render();

        portraitButton.interactable = CanOpenStatus();
    }

    public void Refresh()
    {
        var leader = FindLeader();
        if (!ReferenceEquals(leader, member))
        {
            Unbind();
            member = leader;
            if (member != null)
            {
                member.OnHpChanged += HandleResourceChanged;
                member.OnMpChanged += HandleResourceChanged;
            }
        }
        Render();
    }

    public void OpenStatus()
    {
        if (!CanOpenStatus()) return;
        // This entrance returns directly to exploration rather than the ESC menu.
        statusPanel.Show();
        var members = PartyRuntimeState.PartyMembers;
        for (int i = 0; i < members.Count; i++)
            if (ReferenceEquals(members[i], member)) { statusPanel.SelectMember(i); break; }
    }

    private bool CanOpenStatus()
    {
        return member != null && statusPanel != null && !FieldPauseState.IsPaused &&
            !TutorialPanelController.IsTutorialActive && !statusPanel.ClosedThisFrame;
    }

    private static Character FindLeader()
    {
        foreach (var candidate in PartyRuntimeState.PartyMembers)
            if (candidate != null) return candidate;
        return null;
    }

    private void Render()
    {
        bool hasMember = member != null;
        hudGroup.alpha = hasMember ? 1 : 0;
        hudGroup.blocksRaycasts = hasMember;
        portraitButton.interactable = CanOpenStatus();
        if (!hasMember)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
            hpArc.fillAmount = mpArc.fillAmount = 0;
            downMarker.SetActive(false);
            return;
        }

        shownHp = member.Hp; shownMaxHp = member.MaxHp;
        shownMp = member.Mp; shownMaxMp = member.MaxMp;
        shownDead = member.isDead || member.Hp <= 0;
        hpArc.fillAmount = Ratio(member.Hp, member.MaxHp) * arcSpan;
        mpArc.fillAmount = Ratio(member.Mp, member.MaxMp) * arcSpan;
        portraitImage.sprite = member.Portrait;
        portraitImage.enabled = member.Portrait != null;
        portraitImage.color = shownDead ? new Color(.52f, .52f, .52f) : Color.white;
        downMarker.SetActive(shownDead);
    }

    private static float Ratio(int current, int maximum)
    {
        return maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0;
    }

    private void HandleResourceChanged(int previous, int current) { Refresh(); }

    private void Unbind()
    {
        if (member == null) return;
        member.OnHpChanged -= HandleResourceChanged;
        member.OnMpChanged -= HandleResourceChanged;
        member = null;
    }
}
