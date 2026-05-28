using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyRuntimeState
{
    private static readonly List<Character> partyMembers = new List<Character>();

    public static IReadOnlyList<Character> PartyMembers => partyMembers;
    public static bool HasPartyData => partyMembers.Count > 0;

    public static void InitializeIfEmpty(IEnumerable<Character> initialMembers)
    {
        if (HasPartyData)
            return;

        partyMembers.Clear();

        if (initialMembers == null)
            return;

        foreach (var member in initialMembers)
        {
            if (member == null)
                continue;

            partyMembers.Add(member);
        }

        Debug.Log($"[PartyRunTimeState] Initialized party count={partyMembers.Count}");
    }

    public static void UpdateFromBattleController(IEnumerable<BaseController> controllers)
    {
        List<Character> oldMembers = new List<Character>(partyMembers);
        List<Character> updatedMembers = new List<Character>();

        partyMembers.Clear();

        if (controllers == null)
            return;

        foreach (var ctrl in controllers)
        {
            if (ctrl == null || ctrl.data == null)
                continue;
            if (ctrl.data.Team != Team.Player)
                continue;

            Character updateData = ctrl.data;

            // Prefer stable characterId when matching previous field data, then fall back to Name for old data.
            Character oldData = oldMembers.Find(x =>
                x != null &&
                !string.IsNullOrWhiteSpace(x.characterId) &&
                x.characterId == updateData.characterId);

            if (oldData == null)
                oldData = oldMembers.Find(x => x != null && x.Name == updateData.Name);

            // Battle data may lose portrait references, so restore it from the previous field copy when needed.
            if (updateData.Portrait == null && oldData != null && oldData.Portrait != null)
                updateData.Portrait = oldData.Portrait;

            updatedMembers.Add(ctrl.data);
        }

        // Keep the original party order stable even if battle controllers were removed/re-added by death and revive.
        foreach (var oldMember in oldMembers)
        {
            Character updatedMember = FindMatchingMember(updatedMembers, oldMember);
            if (updatedMember == null)
                continue;

            partyMembers.Add(updatedMember);
            updatedMembers.Remove(updatedMember);
        }

        // Append brand-new members after the original party. This keeps future join flows from being dropped.
        foreach (var updatedMember in updatedMembers)
        {
            if (updatedMember == null)
                continue;

            partyMembers.Add(updatedMember);
        }

        Debug.Log($"[PartyRuntimeState] Updated party count={partyMembers.Count}");
    }

    private static Character FindMatchingMember(List<Character> members, Character target)
    {
        if (members == null || target == null)
            return null;

        if (!string.IsNullOrWhiteSpace(target.characterId))
        {
            Character byId = members.Find(member =>
                member != null &&
                !string.IsNullOrWhiteSpace(member.characterId) &&
                member.characterId == target.characterId);

            if (byId != null)
                return byId;
        }

        return members.Find(member => member != null && member.Name == target.Name);
    }

    public static bool TryHealFirstInjuredAliveMember(int amount, out Character healedMember)
    {
        healedMember = null;

        if (amount <= 0)
            return false;

        foreach (var member in partyMembers)
        {
            if (member == null)
                continue;
            if (member.isDead || member.Hp <= 0)
                continue;
            if (member.Hp >= member.MaxHp)
                continue;

            int prevHp = member.Hp;
            member.Hp = Mathf.Min(member.Hp + amount, member.MaxHp);
            member.NotifyHpChange(prevHp, member.Hp);
            healedMember = member;

            Debug.Log($"[PartyRuntimeState] Healed {member.Name}: {prevHp}->{member.Hp}");
            return true;
        }

        return false;
    }

    public static bool TryHealMember(Character member, int amount)
    {
        if (member == null || amount <= 0)
            return false;
        if (!partyMembers.Contains(member))
            return false;
        if (member.isDead || member.Hp <= 0)
            return false;
        if (member.Hp >= member.MaxHp)
            return false;

        int prevHp = member.Hp;
        member.Hp = Mathf.Min(member.Hp + amount, member.MaxHp);
        member.NotifyHpChange(prevHp, member.Hp);

        Debug.Log($"[PartyRuntimeState] Healed {member.Name}: {prevHp}->{member.Hp}");
        return true;
    }

    public static void Clear()
    {
        partyMembers.Clear();
    }

    public static PartySaveData ToSaveData()
    {
        PartySaveData saveData = new PartySaveData();

        // Export members in current party order. The list index becomes the saved party order.
        foreach (var member in partyMembers)
        {
            PartyMemberSaveData memberSave = new PartyMemberSaveData();
            if (member != null)
            {
                memberSave.characterId = member.characterId;
                memberSave.hp = member.Hp;
                memberSave.maxHp = member.MaxHp;
                memberSave.mp = member.Mp;
                memberSave.maxMp = member.MaxMp;
                memberSave.level = member.Level;
                memberSave.exp = member.Exp;
                memberSave.isDead = member.isDead;
            }

            saveData.members.Add(memberSave);
        }

        return saveData;
    }

    public static void LoadFromSaveData(PartySaveData saveData, CharacterDataBase characterDataBase)
    {
        partyMembers.Clear();

        if (saveData == null || saveData.members == null)
        {
            Debug.LogWarning("[PartyRuntimeState] Load skipped because save data is null.");
            return;
        }

        for (int i = 0; i < saveData.members.Count; i++)
        {
            PartyMemberSaveData memberSave = saveData.members[i];
            if (memberSave == null || string.IsNullOrWhiteSpace(memberSave.characterId))
                continue;

            Character baseCharacter = characterDataBase != null
                ? characterDataBase.FindById(memberSave.characterId)
                : null;

            if (baseCharacter == null)
            {
                Debug.LogWarning($"[PartyRuntimeState] Failed to load party member. characterId={memberSave.characterId}");
                continue;
            }

            // Copy the database character before applying save data so runtime changes do not modify source data.
            Character runtimeMember = baseCharacter.Copy();

            runtimeMember.MaxHp = memberSave.maxHp;
            runtimeMember.Hp = Mathf.Clamp(memberSave.hp, 0, runtimeMember.MaxHp);
            runtimeMember.MaxMp = memberSave.maxMp;
            runtimeMember.Mp = Mathf.Clamp(memberSave.mp, 0, runtimeMember.MaxMp);
            runtimeMember.Level = Mathf.Max(1, memberSave.level);
            runtimeMember.Exp = Mathf.Max(0, memberSave.exp);
            runtimeMember.isDead = memberSave.isDead || runtimeMember.Hp <= 0;

            partyMembers.Add(runtimeMember);
        }

        Debug.Log($"[PartyRuntimeState] Loaded party count={partyMembers.Count}");
    }
}
