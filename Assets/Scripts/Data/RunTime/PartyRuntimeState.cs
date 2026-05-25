using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyRuntimeState
{
    private static readonly List<Character> partyMembers=new List<Character>();

    public static IReadOnlyList<Character> PartyMembers => partyMembers;
    public static bool HasPartyData=>partyMembers.Count > 0;
    public static void InitializeIfEmpty(IEnumerable<Character> initialMembers)
    {
        if (HasPartyData)
            return;
        partyMembers.Clear();

        if (initialMembers == null)
            return;
        foreach(var member in initialMembers)
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

        partyMembers.Clear();

        if (controllers == null)
            return;
        foreach(var ctrl in controllers)
        {
            if (ctrl == null || ctrl.data == null)
                continue;
            if (ctrl.data.Team != Team.Player)
                continue;

            Character updateData = ctrl.data;

            //如果战斗后的数据没有头像
            //而旧数据里有头像
            //那就把旧头像复制给新数据
            //先用 characterId 找
            //如果旧数据没有 id，才 fallback 用 Name
            Character oldData = oldMembers.Find(x => x != null && !string.IsNullOrWhiteSpace(x.characterId)&&x.characterId==updateData.characterId);

            if (updateData.Portrait == null && oldData != null && oldData.Portrait != null)
            {
                updateData.Portrait = oldData.Portrait;
            }

            partyMembers.Add(ctrl.data);
        }
        Debug.Log($"[PartyRuntimeState] Updated party count={partyMembers.Count}");
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
    }    public static void Clear()
    {
        partyMembers.Clear();
    }

    // 按 PartyMembers 当前顺序导出队伍快照。
    // List index 会作为之后读取时的队伍顺序。
    public static PartySaveData ToSaveData()
    {
        PartySaveData saveData = new PartySaveData();

        foreach(var member in partyMembers)
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
}
