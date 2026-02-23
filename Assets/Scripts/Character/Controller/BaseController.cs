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
        Debug.LogError($"[TakeDamage CALLED]{name} dmg={damage}");

        Debug.Log($"[TakeDamage Base] {GetType().Name} {data?.Name} dmg={damage}");
        if (data==null||data.isDead) return;

        int prev = data.Hp;
        data.Hp = Mathf.Max(0, data.Hp - damage);

        data.NotifyHpChange(prev, data.Hp);
        if (data.Hp <= 0)
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

    public virtual void SetTargeted(bool targetd)
    {
        Debug.Log($"{data.Name}targeted={targetd}");
    }
}
