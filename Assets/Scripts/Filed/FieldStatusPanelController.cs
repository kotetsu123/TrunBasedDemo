using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FieldCharacterStatsController))]
public sealed class FieldStatusPanelController : BasePanel
{
    [Header("Party")]
    [SerializeField] private FieldCharacterStatsController statsController;
    [SerializeField] private RectTransform memberRoot;
    [SerializeField] private FieldStatusMemberView memberPrefab;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text partyCountText;

    [Header("Selected Character")]
    [SerializeField] private GameObject detailsRoot;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text mpText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image mpFill;
    [SerializeField] private Image expFill;
    [SerializeField] private Button closeButton;

    private readonly List<FieldStatusMemberView> memberViews = new List<FieldStatusMemberView>();
    private FieldEscMenuPanelController returnMenu;
    private FieldSystemMenuController systemMenu;
    private bool previousPause;
    private int lastCloseFrame = -1;

    public override bool IsOpen => isShowing;
    public bool IsShowing => isShowing;
    public bool ClosedThisFrame => lastCloseFrame == Time.frameCount ||
        (systemMenu != null && systemMenu.ClosedThisFrame);
    private bool isShowing;

    protected override void Awake()
    {
        base.Awake();
        if (statsController == null) statsController = GetComponent<FieldCharacterStatsController>();
        statsController.OnStatsChanged += Render;
        previousButton.onClick.AddListener(PreviousMember);
        nextButton.onClick.AddListener(NextMember);
        closeButton.onClick.AddListener(Close);
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (statsController != null) statsController.OnStatsChanged -= Render;
        previousButton.onClick.RemoveListener(PreviousMember);
        nextButton.onClick.RemoveListener(NextMember);
        closeButton.onClick.RemoveListener(Close);
        currentTween?.Kill();
    }

    private void Update()
    {
        if (!isShowing) return;
        if (systemMenu == null && Input.GetMouseButtonDown(1)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) PreviousMember();
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) NextMember();
    }

    public void OpenFromMenu(FieldEscMenuPanelController menu)
    {
        if (systemMenu != null) { systemMenu.OpenFromMenu(menu); return; }
        if (isShowing) return;
        returnMenu = menu;
        returnMenu?.Hide();
        Show();
    }

    public override void Show()
    {
        if (systemMenu != null) { systemMenu.OpenStatus(); return; }
        if (!isShowing) previousPause = FieldPauseState.IsPaused;
        FieldPauseState.SetPaused(true);
        ShowPage();
    }

    public void BindSystemMenu(FieldSystemMenuController menu) { systemMenu = menu; }

    internal void ShowPage()
    {
        isShowing = true;
        Refresh();
        base.Show();
    }

    public void Refresh()
    {
        statsController.Refresh();
    }

    public void PreviousMember() { statsController.SelectPreviousMember(); }
    public void NextMember() { statsController.SelectNextMember(); }
    public void SelectMember(int index) { statsController.SelectMember(index); }

    public bool TryCloseByEsc()
    {
        if (systemMenu != null) return systemMenu.TryCloseByEsc();
        if (ClosedThisFrame) return true;
        if (!isShowing) return false;
        Close();
        return true;
    }

    public void Close()
    {
        if (systemMenu != null) { systemMenu.Close(); return; }
        if (!isShowing) return;
        Hide();
        lastCloseFrame = Time.frameCount;
        var menu = returnMenu;
        returnMenu = null;
        if (menu != null)
        {
            menu.Show();
            FieldPauseState.SetPaused(true);
        }
        else FieldPauseState.SetPaused(previousPause);
    }

    public override void Hide()
    {
        isShowing = false;
        base.Hide();
    }

    public override void HideImmediate()
    {
        isShowing = false;
        base.HideImmediate();
    }

    private void Render(CharacterStatsSnapshot stats)
    {
        RefreshMembers();
        bool hasMember = stats != null;
        detailsRoot.SetActive(hasMember);
        emptyState.SetActive(!hasMember);
        if (!hasMember) return;

        portrait.sprite = stats.Portrait;
        portrait.enabled = stats.Portrait != null;
        nameText.text = string.IsNullOrWhiteSpace(stats.Name) ? "Unnamed" : stats.Name;
        levelText.text = $"Level {stats.Level}";
        conditionText.text = stats.IsDead ? "KO" : "Active";
        conditionText.color = stats.IsDead ? new Color(.95f,.48f,.43f) : new Color(.53f,.8f,.65f);
        hpText.text = $"{stats.Hp} / {stats.MaxHp}";
        mpText.text = $"{stats.Mp} / {stats.MaxMp}";
        expText.text = $"{stats.Exp} / {stats.ExpToNextLevel}";
        nextLevelText.text = $"To next level   {Mathf.Max(0, stats.ExpToNextLevel - stats.Exp)}";
        attackText.text = stats.Attack.ToString(CultureInfo.InvariantCulture);
        speedText.text = stats.Speed.ToString("0.##", CultureInfo.InvariantCulture);
        SetFill(hpFill, stats.Hp, stats.MaxHp);
        SetFill(mpFill, stats.Mp, stats.MaxMp);
        SetFill(expFill, stats.Exp, stats.ExpToNextLevel);
    }

    private void RefreshMembers()
    {
        var members = statsController.Members;
        int visible = 0;
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null) continue;
            if (visible == memberViews.Count) memberViews.Add(Instantiate(memberPrefab, memberRoot));
            var view = memberViews[visible++];
            view.gameObject.SetActive(true);
            view.Bind(members[i], i, i == statsController.SelectedIndex, SelectMember);
        }
        for (int i = visible; i < memberViews.Count; i++) memberViews[i].gameObject.SetActive(false);
        partyCountText.text = $"Party  /  {visible}";
        previousButton.interactable = nextButton.interactable = visible > 1;
    }

    private static void SetFill(Image fill, int current, int maximum)
    {
        var rect = fill.rectTransform;
        rect.anchorMax = new Vector2(maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0, 1);
    }
}
