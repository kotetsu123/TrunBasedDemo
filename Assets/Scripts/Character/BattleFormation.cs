using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
       Player,
    Enemy
}
[System.Serializable]
public class FormationSlot
{
    public int index;
    public Transform anchor;
    [HideInInspector] public BaseController occupant;//Õ¼¾ÝÕß

    public bool isEmpty => occupant == null;
}

public class BattleFormation : MonoBehaviour
{
    [SerializeField] private FormationSlot[] playerSlots = new FormationSlot[4];
    [SerializeField] private FormationSlot[] enemySlots = new FormationSlot[6];

    public event System.Action<Team,int,BaseController,BaseController> OnSlotChanged;

    public int FindFirstEmpty(Team team)
    {
        var arr = team == Team.Player ? playerSlots : enemySlots;
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

}
