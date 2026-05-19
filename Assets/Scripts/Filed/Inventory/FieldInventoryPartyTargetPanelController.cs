using System;
using UnityEngine;

public class FieldInventoryPartyTargetPanelController : BasePanel
{
    [SerializeField] private FiledPartyHudItem[] items;

    public event Action<Character> OnPartyMemberSelected;

    protected override void Awake()
    {
        base.Awake();
        HideImmediate();
    }

    public override void Show()
    {
        Refresh();
        base.Show();
    }

    private void Refresh()
    {
        var party = PartyRuntimeState.PartyMembers;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (party != null && i < party.Count)
            {
                items[i].Bind(party[i], HandlePartyMemberClicked);
            }
            else
            {
                items[i].Bind(null);
            }
        }
    }

    private void HandlePartyMemberClicked(Character member)
    {
        OnPartyMemberSelected?.Invoke(member);
    }
}
