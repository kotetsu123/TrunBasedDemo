using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FieldSkillItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image selectionMark;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scopeText;
    [SerializeField] private TMP_Text costText;
    private SkillData skill;
    private Action<SkillData> select;

    private void Awake() { button.onClick.AddListener(Select); }
    private void OnDestroy() { button.onClick.RemoveListener(Select); }
    public void Bind(Character caster, SkillData value, bool selected, Action<SkillData> callback)
    {
        skill = value;
        select = callback;
        bool field = FieldSkillService.IsFieldSkill(value);
        bool ready = string.IsNullOrEmpty(FieldSkillService.UnavailableReason(caster, value));
        nameText.text = value.skillName;
        costText.text = value.mpCost + " MP";
        scopeText.text = field ? FieldSkillService.TargetLabel(value) : "Battle only";
        nameText.color = ready ? new Color(.89f, .93f, .91f) : new Color(.46f, .5f, .49f);
        scopeText.color = costText.color = ready ? new Color(.65f, .75f, .71f) : new Color(.42f, .46f, .45f);
        background.color = selected ? new Color(.15f, .25f, .22f) : new Color(.075f, .1f, .095f);
        selectionMark.gameObject.SetActive(selected);
        selectionMark.color = field ? new Color(.81f, .72f, .43f) : new Color(.45f, .48f, .47f);
        // Read-only combat skills remain selectable for details, but never castable here.
        button.interactable = true;
    }
    private void Select() { if (skill != null) select?.Invoke(skill); }
}
