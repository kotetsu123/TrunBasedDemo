using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    public  Character data;

    [Header("Visual")]
    public Sprite portrait;//角色肖像

   private float _nextDmgLogTime = 0f;

    public abstract bool isPlayer { get; }
   // public abstract bool isDead { get; }
    public virtual bool isDead=>data.isDead;


    protected virtual void Awake()
    {
        EnsureRunTimeDefaults();
    }
    //统一的受伤逻辑
    public virtual void TakeDamage(int damage)
    {
        if (Time.unscaledTime >= _nextDmgLogTime)
        {
            _nextDmgLogTime = Time.unscaledTime + 0.3f;
            Debug.Log($"[DMG] {GetType().Name} {data?.Name} {data?.Hp}/{data?.MaxHp} hash={(data == null ? 0 : data.GetHashCode())}");
        }

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
    public void Heal(int amout)
    {
        int prevHp = data.Hp;
        data.Hp = Mathf.Min(data.MaxHp, data.Hp + amout);
        data.NotifyHpChange(prevHp, data.Hp);
    }
    //给子类"拓展"的钩子
    protected virtual void OnDeath()
    {
        BattleManager.Instance.NotifyDeath(this);
    }
    public virtual  void Init(Character data)
    {
        //Debug.LogError($"[Init CALLED]{gameObject.name}");

        this.data = data;
        //基础战斗状态
        this.data.isDead = false;
        this.data.ActionValue = this.data.MaxActionValue;

        //hp/MaxHp初始化兜底（防止MaxHp=0）
        if(this.data.MaxHp<=0&&this.data.Hp>0)
            this.data.Hp= this.data.MaxHp;

        //最后兜底：两个都<=0 就给个默认值，避免除0和UI全0
        if (this.data.MaxHp <= 0)
            this.data.MaxHp = 100;
        if(this.data.Hp<=0)
            this.data.Hp=this.data.MaxHp;
    }

    public virtual void SetTargeted(bool targetd)
    {
        Debug.Log($"{data.Name}targeted={targetd}");
    }
    private void EnsureRunTimeDefaults()
    {
        if (data == null) return;
         
        data.isDead = false;

        if(data.MaxHp<=0&&data.Hp>0)data.MaxHp= data.Hp;
        if(data.Hp<=0&&data.MaxHp>0)data.Hp= data.MaxHp;
        if (data.MaxHp <= 0) data.MaxHp = 100;
        if (data.Hp <= 0) data.Hp = data.MaxHp;

        if(data.MaxActionValue>0)data.ActionValue= data.MaxActionValue;
    }
    //使用技能
    public void UseSkill(SkillData skill, BaseController target)
    {
        if (data.Mp < skill.mpCost)
        {
            Debug.Log("Not enough MP");
            return;
        }
        int prevMp = data.Mp; 
        data.Mp-= skill.mpCost;
        data.Mp = Mathf.Max(0, data.Mp);
        data.NotifyMpChange(prevMp, data.Mp);

        switch (skill.skillType)
        {
            case SkillType.Damage:
                {
                    int damage = data.Attack + skill.power;
                    target.TakeDamage(damage);
                    Debug.Log($"{data.Name} used {skill.skillName} on {target.data.Name}");
                    break;
                }
            case SkillType.Heal:
                {
                    Debug.Log("Heal branch entered");
                    this.Heal(skill.power);
                    //target.Heal(skill.power);
                    Debug.Log($"[SkillType HEAL]{data.Name} healed {target.data.Name} for {skill.power}");
                    break;
                }
            case SkillType.Revive:
                {
                   // target.Revive();
                    break;
                }

        }
       

        
    }
}
