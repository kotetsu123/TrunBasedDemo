using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerHpHudItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;

    private BaseController _ctrl;

    public BaseController Bount=> _ctrl;

    public void Bind(BaseController ctrl)
    {
        _ctrl = ctrl;
        Refresh();
    }
    public void Refresh()
    {
        if (_ctrl == null || _ctrl.data == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if(nameText!=null)nameText.text = _ctrl.data.Name;
        if(hpText!=null)hpText.text = $" {_ctrl.data.Hp}/{_ctrl.data.maxHp}";
    }
}
