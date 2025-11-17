using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : BaseController
{

    public HpBar hpbar;

    public override bool isPlayer => true;
    public override bool isDead => data.isDead;

    public override void TakeDamage(int damage)
    {
        data.Hp -= damage;
        if (data.Hp < 0)
        {
            data.Hp = 0;
        }
        //TODO:此处更新playerUHD UI逻辑

        if (data.Hp == 0)
        {
            data.isDead = true;
        }


        //TODO:Hp 分离逻辑跟怪物的不一样
        /*hpbar.UpdateHp();
        if (data.isDead)
        {
            hpbar.Hide();
        }*/

    }

    private void Start()
    {
        /*if (data == null)
        {
            data = new Character("Player", 200, 50, 90, 200f);
            data.Team = Character.TeamType.Player;
            data.isPlayer = true;
            BatteleManager.Instance.RigisterCharacter(data);
            Debug.Log($"[PlayerController]Awake over:{data != null}");
        }*/

        BatteleManager.Instance.RigisterCharacter(this);
        //Debug.Log("PlayerController has rasing "  );
    }
}


