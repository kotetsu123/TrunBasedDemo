using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    public  Character data;

    [Header("Visual")]
    public Sprite portait;//角色肖像

   
    public abstract bool isPlayer { get; }
   // public abstract bool isDead { get; }
    public virtual bool isDead=>data.isDead;

    //统一的受伤逻辑
    public virtual void TakeDamage(int damage)
    {
        if (data.isDead) return;
        data.Hp = Mathf.Max(0, data.Hp - damage);
        if (data.Hp == 0)
        {
            data.isDead = true;
            OnDeath();
        }

    }

    //给子类"拓展"的钩子
    protected virtual void OnDeath()
    {
        BattleManager.Instance.NotifyDeath(this);
    }
    public virtual  void Init(Character data)
    {
        this.data = data;
        this.data.isDead = false;
        this.data.ActionValue = this.data.MaxActionValue;
    }
}
