using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    public Action<BaseController> OnRevied;

    public Character data;

    public int healUsedCount = 0;


    /// <summary>
    /// TODO:��ȡ��������ĳɴ�����������Controllerֻ������ֺ͵��ü����߼������ݶ�����Character������������Unity�����߼�����
    /// </summary>
    [Header("Visual")]
    public Sprite portrait;//��ɫФ��

    private float _nextDmgLogTime = 0f;

    [SerializeField] private List<SkillData> skills;

    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform floatringTexAnchor;
    public IReadOnlyList<SkillData> Skills => skills;
    public abstract bool isPlayer { get; }
   // public abstract bool isDead { get; }
    public virtual bool isDead=>data.isDead;


    protected virtual void Awake()
    {
        EnsureRunTimeDefaults();
    }
    //ͳһ�������߼�
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

        int actualDamage = prev - data.Hp;
        if (actualDamage > 0)
        {
            ShowFloatingText($"-{actualDamage}", Color.red);
        }
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

        int actualHeal = data.Hp - prevHp;
        if (actualHeal > 0)
        {
            ShowFloatingText($"+{actualHeal}",Color.green);
        }

        data.NotifyHpChange(prevHp, data.Hp);
    }
    public void RestoreMp(int amount)
    {
        if (data == null || amount <= 0) return;

        int prevMp = data.Mp;
        data.Mp = Mathf.Min(data.MaxMp, data.Mp + amount);

        int actualRestore = data.Mp - prevMp;
        if (actualRestore > 0)
        {
            ShowFloatingText($"+{actualRestore} MP", Color.cyan);
        }

        data.NotifyMpChange(prevMp, data.Mp);
    }
    public void Revive(int amount)
    {
        if (data == null) return;

        bool wasDead=data.isDead||isDead||data.Hp<=0;
        if (!wasDead) return;

        data.isDead=false;
        //isDead=false;

        int prevHp= data.Hp;
        data.Hp = Mathf.Clamp(amount, 1, data.MaxHp);

        ShowFloatingText($"+{data.Hp}",Color.green);

        data.NotifyHpChange(prevHp, data.Hp);

        OnRevied?.Invoke(this);

    }
    //������"��չ"�Ĺ���
    protected virtual void OnDeath()
    {
        BattleManager.Instance.NotifyDeath(this);
    }
    public virtual  void Init(Character data)
    {
        //Debug.LogError($"[Init CALLED]{gameObject.name}");

        this.data = data;
        //����ս��״̬
        this.data.ActionValue = this.data.MaxActionValue;
        NormalizeRuntimeHpState();
        if (this.data.Portrait == null && portrait != null)
        {
            this.data.Portrait = portrait;
        }

    }

    public virtual void SetTargeted(bool targetd)
    {
        Debug.Log($"{data.Name}targeted={targetd}");
    }
    private void EnsureRunTimeDefaults()
    {
        if (data == null) return;

        NormalizeRuntimeHpState();

        if(data.MaxActionValue>0)data.ActionValue= data.MaxActionValue;
    }

    private void NormalizeRuntimeHpState()
    {
        if (data == null) return;

        // MaxHp 没填但 Hp 有值时，用当前 Hp 反推 MaxHp，兼容旧配置。
        if (data.MaxHp <= 0 && data.Hp > 0)
            data.MaxHp = data.Hp;

        // MaxHp 和 Hp 都无效时，说明这更像是数据没配置，不是合法死亡状态。
        if (data.MaxHp <= 0)
        {
            data.MaxHp = 100;
            data.Hp = data.MaxHp;
            data.isDead = false;
            return;
        }

        // 玩家 Hp=0 是合法的死亡状态，不能在进入下一场战斗时被 Init 自动补满。
        if (data.Hp <= 0)
        {
            if (data.isDead || data.Team == Team.Player || isPlayer)
            {
                data.Hp = 0;
                data.isDead = true;
            }
            else
            {
                data.Hp = data.MaxHp;
                data.isDead = false;
            }

            return;
        }

        data.Hp = Mathf.Min(data.Hp, data.MaxHp);
        data.isDead = false;
    }
    //ʹ�ü���
    public void UseSkill(SkillData skill, BaseController target)
    {
        Debug.Log($"[UseSkill] actor={data.Name}, target={target?.data.Name}, skill={skill.skillName}, type={skill.skillType}, targetType={skill.targetType}");
        if (data.Mp < skill.mpCost)
        {
            Debug.Log("Not enough MP");
            return;
        }
        BattleManager.Instance?.ShowSkillName(skill.skillName);

        int prevMp = data.Mp; 
        data.Mp-= skill.mpCost;
        data.Mp = Mathf.Max(0, data.Mp);
        data.NotifyMpChange(prevMp, data.Mp);

       // bool revivedTarget = false;

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
                    target.Heal(skill.power);
                    if (data.Team == Team.Enemy)
                    {
                        healUsedCount++;
                    }
                    Debug.Log($"[SkillType HEAL]{data.Name} healed {target.data.Name} for {skill.power}");
                    break;
                }
            case SkillType.Revive:
                {
                  target.Revive(skill.power);
                    break;
                }

        }  
    }
    protected void ShowFloatingText(string message,Color color)
    {
        if (floatingTextPrefab == null) return;

        Vector3 spawnPos = floatringTexAnchor != null 
            ? floatringTexAnchor.position 
            : transform.position + Vector3.up * 2f;

        spawnPos += new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0f, 0f);

        GameObject obj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        FloatingText ft=obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.SetUp(message,color);
        }        
    }
    protected void ShowFloatingText(string message)
    {
        ShowFloatingText(message,Color.white);
    }
}
