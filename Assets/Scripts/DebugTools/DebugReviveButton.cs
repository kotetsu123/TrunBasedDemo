using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class DebugReviveButton : MonoBehaviour
{
    [SerializeField] private BattleManager battle;
    [SerializeField] private BattleFormation formation;
    [SerializeField] private int reviveHp = 10;

    public void ReviveFirstDowendPlayer()
    {
        if (battle == null) return;

        for(int i = 0; i < 4; i++)
        {
            var p = formation.GetPlaeyrAsSlot(i);
            if (p == null || p.data == null) return;

            if (p.data.isDead || p.isDead || p.data.Hp <= 0)
            {
                battle.Revive(p, reviveHp);
                return;
            }
        }
    }
}
