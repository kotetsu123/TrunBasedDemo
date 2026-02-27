using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : BaseController
{

    //public HpBar hpbar;
    public PlayerHpHudItem playerHud;
   

    

    public override bool isPlayer => true;
    public override bool isDead => data.isDead;

    public override void TakeDamage(int damage)
    {
      
        base.TakeDamage(damage);   
        //TODO:此处更新playerUHD UI逻辑

        


        //TODO:Hp 分离逻辑跟怪物的不一样
        /*hpbar.UpdateHp();
        if (data.isDead)
        {
            hpbar.Hide();
        }*/

    }
    protected override void OnDeath()
    {
        
        base.OnDeath();
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
        //TODO:等spawner 逻辑完善后一定要注释掉这条。防止跟battlemanager /或者spawner 产生重复注册

        BattleManager.Instance.RegisterCharacter(this);
        
       // playerHud.gameObject.SetActive(true);
       // playerHud.Bind(this);
       // playerHud.UpdateHp();


      
        //Debug.Log("PlayerController has rasing "  );
    }
}


