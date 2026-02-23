using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpHud : MonoBehaviour
{
    public Image fill;
    public Image character_pic;


    private BaseController owner;
   
    public void Bind(BaseController character)
    {
        owner = character;
        UpdateHp();
        if (owner != null)
        {
            Debug.Log($"PlayerHUD捆绑的名字是：{owner.data.Name}");
        }
    }
    public void UpdateHp()
    {
        if (owner == null)
        {
            return;
        }
        float ratio = (float)owner.data.Hp / owner.data.MaxHp;
        fill.fillAmount = Mathf.Clamp01(ratio);
    }
    public void Isdead()
    {
        //TODO: 需要在数据类？ 又或者是basecontroller 当中添加角色图片。
        character_pic.color = Color.gray;
    }
}
