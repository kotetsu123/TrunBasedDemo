using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FieldStatusMemberView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image portrait;
    [SerializeField] private Image background;
    [SerializeField] private GameObject selectionMark;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text summaryText;

    private int memberIndex;
    private Action<int> onSelected;

    private void Awake()
    {
        button.onClick.AddListener(Select);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(Select);
    }

    public void Bind(Character member, int index, bool selected, Action<int> callback)
    {
        memberIndex = index;
        onSelected = callback;
        portrait.sprite = member.Portrait;
        portrait.enabled = member.Portrait != null;
        nameText.text = string.IsNullOrWhiteSpace(member.Name) ? "Unnamed" : member.Name;
        bool down = member.isDead || member.Hp <= 0;
        summaryText.text = down ? $"Lv. {member.Level}   KO" : $"Lv. {member.Level}   {member.Hp}/{member.MaxHp} HP";
        summaryText.color = down ? new Color(.95f, .48f, .43f) : new Color(.66f, .73f, .71f);
        background.color = selected ? new Color(.15f, .25f, .22f) : new Color(.09f, .115f, .11f);
        selectionMark.SetActive(selected);
    }

    private void Select()
    {
        onSelected?.Invoke(memberIndex);
    }
}
