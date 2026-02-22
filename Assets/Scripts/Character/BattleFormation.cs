using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class FormationSlot
{
    public int index;
    public Transform anchor;
    [HideInInspector] public BaseController occupant;//占据者

    public bool isEmpty => occupant == null;
}

public class BattleFormation : MonoBehaviour
{
    [SerializeField] private FormationSlot[] playerSlots = new FormationSlot[4];
    [SerializeField] private FormationSlot[] enemySlots = new FormationSlot[5];

    public event System.Action<Team,int,BaseController,BaseController> OnSlotChanged;

    private static readonly int[] EnemyPreferredOrder = { 2, 3, 1, 4, 0 };

    public int FindFirstEmpty(Team team)
    {
        var arr = team == Team.Player ? playerSlots : enemySlots;

        if (team == Team.Enemy)
        {
            for(int i = 0; i < EnemyPreferredOrder.Length; i++)
            {
                int idx = EnemyPreferredOrder[i];
                if (idx < 0 || idx >= arr.Length) continue;
                if (arr[idx].occupant == null) return idx;
            }
            return -1;
        }

        for (int i = 0; i < arr.Length;i++)
        {
            if (arr[i].isEmpty) return i;
        }
        return -1;
    }

    public Transform GetAnchor(Team team,int slotIndex)
    {
        var arr=team==Team.Player?playerSlots: enemySlots;
        return arr[slotIndex].anchor;
    }
    public bool TryOccupy(Team team, int slotIndex,BaseController ctrl)
    {
        var arr=team==Team.Player? playerSlots : enemySlots;
        var slot = arr[slotIndex];
        if (!slot.isEmpty) return false;

        var prev = slot.occupant;
        slot.occupant = ctrl;
        OnSlotChanged?.Invoke(team, slotIndex, prev, ctrl);
        return true;
    }
    public bool Release(BaseController ctrl,out (Team team,int slotIndex) released)
    {
        //player
        for(int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].occupant == ctrl)
            {
                playerSlots[i].occupant = null;
                released = (Team.Player, i);
                OnSlotChanged?.Invoke(Team.Player, i, ctrl, null);
                return true;
            }
        }
        //Enmey
        for(int i = 0; i < enemySlots.Length; i++)
        {
            if (enemySlots[i].occupant == ctrl)
            {
                enemySlots[i].occupant = null;
                released = (Team.Enemy, i);
                OnSlotChanged?.Invoke(Team.Enemy, i, ctrl, null);
                return true;
            }
        }
        released = default;
        return false;
    }
    public List<BaseController> GetAliveEnemiesInPreferredOrder()
    {
        var result=new List<BaseController>(5);

        //用已有的优先级数组来排序敌人
        int[] order = { 2, 3, 1, 4, 0 };

        for(int i = 0; i < order.Length; i++)
        {
            int idx= order[i];
            var occ=enemySlots[idx].occupant;
            if (occ!=null&&occ.data!=null&&!occ.data.isDead&&!occ.isDead)
                result.Add(occ);        
        }

        return result;
    }
    public List<BaseController> GetPlayersInSlotOrder()
    {
        var result=new List<BaseController>(4);

        for (int i = 0; i <playerSlots.Length; i++)
        {
            var occ= playerSlots[i].occupant;

            if (occ != null && occ.data != null && !occ.data.isDead && !occ.isDead)
                result.Add(occ);
        }
        return result;
    }

}
