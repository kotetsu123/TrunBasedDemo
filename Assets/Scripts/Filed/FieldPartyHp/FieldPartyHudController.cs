using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldPartyHudController : MonoBehaviour
{
    [SerializeField]private FiledPartyHudItem[] items;

    private void Start()
    {
        Refresh();
    }
    public void Refresh()
    {
        var party = PartyRuntimeState.PartyMembers;

        for(int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;
            if (party!=null&&i < party.Count)
            {
                items[i].Bind(party[i]);
            }
            else
            {
                items[i].Bind(null);
            }
        }
    }
}
