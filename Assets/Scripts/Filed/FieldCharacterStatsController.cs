using System;
using System.Collections.Generic;
using UnityEngine;

public class FieldCharacterStatsController : MonoBehaviour
{
    public event Action<CharacterStatsSnapshot> OnStatsChanged;

    public IReadOnlyList<Character> Members => PartyRuntimeState.PartyMembers;
    public int SelectedIndex { get; private set; } = -1;
    public Character SelectedMember => selectedMember;
    public CharacterStatsSnapshot CurrentStats { get; private set; }

    private Character selectedMember;
    private string selectedCharacterId;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Refresh()
    {
        var members = Members;
        int index = FindSelectedIndex(members);

        if (index < 0 && SelectedIndex >= 0 && SelectedIndex < members.Count && members[SelectedIndex] != null)
            index = SelectedIndex;

        if (index < 0)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null)
                    continue;

                index = i;
                break;
            }
        }

        Bind(index);
    }

    public bool SelectMember(int index)
    {
        var members = Members;
        if (index < 0 || index >= members.Count || members[index] == null)
            return false;

        Bind(index);
        return true;
    }

    public bool SelectMember(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return false;

        var members = Members;
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null && members[i].characterId == characterId)
                return SelectMember(i);
        }

        return false;
    }

    public bool SelectNextMember()
    {
        return SelectRelativeMember(1);
    }

    public bool SelectPreviousMember()
    {
        return SelectRelativeMember(-1);
    }

    public void OnClickMember(int index)
    {
        SelectMember(index);
    }

    public void OnClickNextMember()
    {
        SelectNextMember();
    }

    public void OnClickPreviousMember()
    {
        SelectPreviousMember();
    }

    private bool SelectRelativeMember(int direction)
    {
        var members = Members;
        int index = FindSelectedIndex(members);
        if (index < 0)
            index = direction > 0 ? -1 : 0;

        for (int step = 1; step <= members.Count; step++)
        {
            int nextIndex = (index + direction * step + members.Count) % members.Count;
            if (members[nextIndex] != null)
                return SelectMember(nextIndex);
        }

        Refresh();
        return false;
    }

    private int FindSelectedIndex(IReadOnlyList<Character> members)
    {
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            if (member == null)
                continue;

            if (!string.IsNullOrWhiteSpace(selectedCharacterId) && member.characterId == selectedCharacterId)
                return i;
            if (member == selectedMember)
                return i;
        }

        return -1;
    }

    private void Bind(int index)
    {
        Unsubscribe();
        SelectedIndex = index;
        selectedMember = index >= 0 ? Members[index] : null;
        selectedCharacterId = selectedMember?.characterId;

        if (selectedMember != null && isActiveAndEnabled)
        {
            selectedMember.OnHpChanged += HandleResourceChanged;
            selectedMember.OnMpChanged += HandleResourceChanged;
        }

        CurrentStats = selectedMember != null ? new CharacterStatsSnapshot(selectedMember) : null;
        OnStatsChanged?.Invoke(CurrentStats);
    }

    private void HandleResourceChanged(int previous, int current)
    {
        Refresh();
    }

    private void Unsubscribe()
    {
        if (selectedMember == null)
            return;

        selectedMember.OnHpChanged -= HandleResourceChanged;
        selectedMember.OnMpChanged -= HandleResourceChanged;
    }
}
