using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FieldSkillPanelController : BasePanel
{
    [SerializeField] private RectTransform memberRoot;
    [SerializeField] private FieldStatusMemberView memberPrefab;
    [SerializeField] private RectTransform skillRoot;
    [SerializeField] private FieldSkillItemView skillPrefab;
    [SerializeField] private ScrollRect skillScroll;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private TMP_Text resourceText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private GameObject detailsRoot;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text availabilityText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject targetOverlay;
    [SerializeField] private RectTransform targetRoot;
    [SerializeField] private TMP_Text targetTitle;
    [SerializeField] private TMP_Text targetMessage;
    [SerializeField] private Button cancelTargetButton;

    private readonly List<FieldStatusMemberView> members = new List<FieldStatusMemberView>();
    private readonly List<FieldStatusMemberView> targets = new List<FieldStatusMemberView>();
    private readonly List<FieldSkillItemView> skills = new List<FieldSkillItemView>();
    private readonly List<Character> observed = new List<Character>();
    private FieldSystemMenuController systemMenu;
    private FieldCharacterStatsController selection;
    private SkillData selectedSkill;
    private Character previousMember;
    private bool showing, selectingTarget, usingSkill;
    private int lastCancelFrame = -1, lastUseFrame = -1;
    public override bool IsOpen => showing;
    public bool IsSelectingTarget => selectingTarget;
    public Character SelectedMember => selection != null ? selection.SelectedMember : null;
    public SkillData SelectedSkill => selectedSkill;

    protected override void Awake()
    {
        base.Awake();
        previousButton.onClick.AddListener(PreviousMember);
        nextButton.onClick.AddListener(NextMember);
        useButton.onClick.AddListener(UseSelectedSkill);
        closeButton.onClick.AddListener(Close);
        cancelTargetButton.onClick.AddListener(CancelTargetSelection);
        HideImmediate();
    }

    public void BindSystemMenu(FieldSystemMenuController menu, FieldCharacterStatsController controller)
    {
        Unsubscribe();
        systemMenu = menu;
        selection = controller;
        if (closeButton != null) closeButton.gameObject.SetActive(menu == null);
        if (showing) Refresh();
    }

    public void ShowPage()
    {
        showing = true;
        messageText.text = string.Empty;
        Refresh();
        base.Show();
    }

    public void Refresh()
    {
        if (!showing) return;
        Unsubscribe();
        if (selection == null) { Render(null); return; }
        selection.OnStatsChanged += Render;
        foreach (var member in PartyRuntimeState.PartyMembers)
        {
            if (member == null || observed.Contains(member)) continue;
            observed.Add(member);
            member.OnHpChanged += OnResourceChanged;
            member.OnMpChanged += OnResourceChanged;
        }
        selection.Refresh();
    }

    private void Update()
    {
        if (!showing || selectingTarget) return;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) PreviousMember();
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) NextMember();
    }

    public void SelectMember(int index) { if (!selectingTarget) selection?.SelectMember(index); }
    public void PreviousMember() { if (!selectingTarget) selection?.SelectPreviousMember(); }
    public void NextMember() { if (!selectingTarget) selection?.SelectNextMember(); }
    public void SelectSkill(SkillData skill)
    {
        if (!showing || selectingTarget || !FieldSkillService.OwnsSkill(SelectedMember, skill)) return;
        selectedSkill = skill;
        messageText.text = string.Empty;
        Render(selection.CurrentStats);
    }

    private void Render(CharacterStatsSnapshot snapshot)
    {
        if (!showing || usingSkill) return;
        var caster = SelectedMember;
        if (!ReferenceEquals(previousMember, caster))
        {
            selectingTarget = false;
            targetOverlay.SetActive(false);
            selectedSkill = null;
            previousMember = caster;
            messageText.text = string.Empty;
            skillScroll.verticalNormalizedPosition = 1f;
        }
        RefreshMembers();
        characterText.text = caster != null ? caster.Name + "  /  Lv. " + caster.Level : "No party members";
        resourceText.text = caster != null ? $"HP {caster.Hp}/{caster.MaxHp}     MP {caster.Mp}/{caster.MaxMp}" : string.Empty;
        var known = new List<SkillData>();
        if (caster?.Skills != null)
            foreach (var skill in caster.Skills) if (skill != null && !known.Contains(skill)) known.Add(skill);
        if (!known.Contains(selectedSkill)) selectedSkill = known.Count > 0 ? known[0] : null;
        for (int i = 0; i < known.Count; i++)
        {
            if (i == skills.Count) skills.Add(Instantiate(skillPrefab, skillRoot));
            skills[i].gameObject.SetActive(true);
            skills[i].Bind(caster, known[i], known[i] == selectedSkill, SelectSkill);
        }
        for (int i = known.Count; i < skills.Count; i++) skills[i].gameObject.SetActive(false);
        emptyText.gameObject.SetActive(known.Count == 0);
        emptyText.text = caster == null ? "No party members." : "No skills assigned.";
        detailsRoot.SetActive(selectedSkill != null);
        useButton.interactable = false;
        if (selectedSkill != null)
        {
            skillNameText.text = selectedSkill.skillName;
            descriptionText.text = FieldSkillService.Description(caster, selectedSkill);
            costText.text = selectedSkill.mpCost + " MP";
            targetText.text = FieldSkillService.TargetLabel(selectedSkill);
            string reason = FieldSkillService.UnavailableReason(caster, selectedSkill);
            availabilityText.text = reason.Length == 0 ? "Available in exploration" : reason;
            availabilityText.color = reason.Length == 0 ? new Color(.53f, .8f, .65f) : new Color(.65f, .67f, .66f);
            useButton.interactable = !selectingTarget && reason.Length == 0;
        }
        if (selectingTarget) RefreshTargets();
    }

    private void RefreshMembers()
    {
        int count = 0;
        var party = PartyRuntimeState.PartyMembers;
        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] == null) continue;
            if (count == members.Count) members.Add(Instantiate(memberPrefab, memberRoot));
            var view = members[count++];
            view.gameObject.SetActive(true);
            view.Bind(party[i], i, ReferenceEquals(party[i], SelectedMember), SelectMember);
        }
        for (int i = count; i < members.Count; i++) members[i].gameObject.SetActive(false);
        previousButton.interactable = nextButton.interactable = count > 1 && !selectingTarget;
    }

    public void UseSelectedSkill()
    {
        if (!showing || selectingTarget || usingSkill || lastUseFrame == Time.frameCount
            || FieldSkillService.UnavailableReason(SelectedMember, selectedSkill).Length > 0) return;
        if (selectedSkill.targetType == SkillTargetType.Self || selectedSkill.targetType == SkillTargetType.AllyAll)
            ApplySkill(null);
        else
        {
            selectingTarget = true;
            targetOverlay.SetActive(true);
            targetTitle.text = selectedSkill.skillName + "  /  " + FieldSkillService.TargetLabel(selectedSkill);
            targetMessage.text = string.Empty;
            Render(selection.CurrentStats);
        }
    }

    private void RefreshTargets()
    {
        int count = 0;
        var party = PartyRuntimeState.PartyMembers;
        string reason = FieldSkillService.UnavailableReason(SelectedMember, selectedSkill);
        if (reason.Length > 0) targetMessage.text = reason;
        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] == null) continue;
            if (count == targets.Count) targets.Add(Instantiate(memberPrefab, targetRoot));
            var view = targets[count++];
            view.gameObject.SetActive(true);
            view.Bind(party[i], i, false, SelectTarget);
            view.GetComponent<Button>().interactable = reason.Length == 0 && FieldSkillService.CanTarget(SelectedMember, selectedSkill, party[i]);
        }
        for (int i = count; i < targets.Count; i++) targets[i].gameObject.SetActive(false);
    }

    public void SelectTarget(int index)
    {
        var party = PartyRuntimeState.PartyMembers;
        if (!showing || !selectingTarget || index < 0 || index >= party.Count) return;
        ApplySkill(party[index]);
    }

    private void ApplySkill(Character target)
    {
        if (usingSkill || lastUseFrame == Time.frameCount) return;
        usingSkill = true;
        bool success;
        string message;
        try { success = FieldSkillService.TryUse(SelectedMember, selectedSkill, target, out message); }
        finally { usingSkill = false; }
        if (success)
        {
            lastUseFrame = Time.frameCount;
            selectingTarget = false;
            targetOverlay.SetActive(false);
            messageText.text = message;
        }
        else if (selectingTarget) targetMessage.text = message;
        else messageText.text = message;
        Refresh();
    }

    public bool TryCancelTargetSelection()
    {
        if (lastCancelFrame == Time.frameCount) return true;
        if (!selectingTarget) return false;
        CancelTargetSelection();
        return true;
    }
    public void CancelTargetSelection()
    {
        if (!selectingTarget) return;
        lastCancelFrame = Time.frameCount;
        selectingTarget = false;
        targetOverlay.SetActive(false);
        Render(selection != null ? selection.CurrentStats : null);
    }
    public void Close() { systemMenu?.Close(); }
    public override void Hide() { HideImmediate(); }
    public override void HideImmediate()
    {
        showing = selectingTarget = false;
        if (targetOverlay != null) targetOverlay.SetActive(false);
        Unsubscribe();
        base.HideImmediate();
    }
    private void OnDisable() { HideImmediate(); }
    private void Unsubscribe()
    {
        if (selection != null) selection.OnStatsChanged -= Render;
        foreach (var member in observed)
        {
            member.OnHpChanged -= OnResourceChanged;
            member.OnMpChanged -= OnResourceChanged;
        }
        observed.Clear();
    }
    private void OnResourceChanged(int before, int after)
    {
        if (showing && !usingSkill) selection?.Refresh();
    }
    private void OnDestroy()
    {
        Unsubscribe();
        previousButton.onClick.RemoveListener(PreviousMember);
        nextButton.onClick.RemoveListener(NextMember);
        useButton.onClick.RemoveListener(UseSelectedSkill);
        closeButton.onClick.RemoveListener(Close);
        cancelTargetButton.onClick.RemoveListener(CancelTargetSelection);
        currentTween?.Kill();
    }
}
