using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHpHudUI : MonoBehaviour
{
    [SerializeField] private BattleFormation formation;
    [SerializeField] private PlayerHpHudItem[] slots;//4¸ö

    private void Update()
    {
        if (formation == null) return;

        var players = formation.GetPlayersInSlotOrder();

        for(int i = 0; i < slots.Length; i++)
        {
            if (i < players.Count)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].Bind(players[i]);
                slots[i].Refresh();
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }
}
