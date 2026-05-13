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
            Character oldData = oldMembers.Find(x => x != null && x.Name == updateData.Name);

            if (updateData.Portrait == null && oldData != null && oldData.Portrait != null)
            {
                updateData.Portrait = oldData.Portrait;
            }

            partyMembers.Add(ctrl.data);
        }
        Debug.Log($"[PartyRuntimeState] Updated party count={partyMembers.Count}");
    }

    public static void Clear()
    {
        partyMembers.Clear();
    }
}
