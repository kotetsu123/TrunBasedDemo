using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHpHudUI : MonoBehaviour
{
    [SerializeField] private BattleFormation formation;
    [SerializeField] private PlayerHpHudItem[] slots;//4¸ö

    private void Start()
    {
        ReBuild();
    }
    private void OnEnable()
    {
        if (formation != null)
            formation.OnSlotChanged += HandleSlotChange;
    }
    private void OnDisable()
    {
        if(formation!=null)
            formation.OnSlotChanged -= HandleSlotChange;
    }
   
    public void ReBuild()
    {
        if (formation == null) return;

        for(int i=0;i<slots.Length; i++)
        {
            var ctrl = formation.GetPlaeyrAsSlot(i);

            if (ctrl != null && ctrl.data != null)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].Bind(ctrl);//bindÄÚ²¿¶©ÔÄOnHpChanged+Refresh
            }
            else
            {
                slots[i].Bind(null);
                slots[i].gameObject.SetActive(false);
            }
        }
        
    }
    private void HandleSlotChange(Team team,int slotIndex,BaseController prev,BaseController cur)
    {
        if (team != Team.Player) return;
        ReBuild();
    }
}
