using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

[Serializable]
public class EnemySkillWeightEntry
{
    [SerializeField] private SkillData skill;

    [SerializeField, Min(0)]
    private int weight = 1;

    public SkillData Skill => skill;
    public int Weight => Mathf.Max(0,weight);
    public EnemySkillWeightEntry Copy()
    {
        return new EnemySkillWeightEntry
        {
            skill = this.skill,
            weight = this.weight
        };
    }
}


[Serializable]
public class Character

{
    public string characterId;//角色id，唯一标识符

    public Team Team;
    public event Action<int, int> OnHpChanged;//prev ,cur
    public event Action<int, int> OnMpChanged;

    public string Name;

    public int MaxHp;
    public int Hp;

    public int MaxMp;
    public int Mp;

    public int Level = 1;
    public int Exp = 0;//当前等级内经验

    public int Attack;
    public float Speed;
    public float ActionValue;//行动值
    public float MaxActionValue = 200f;//最大行动值//初始行动值

    public Sprite Portrait;

    [Header("Battle skills")]
    [SerializeField]private List<SkillData> skills = new List<SkillData>();

    public IReadOnlyList<SkillData> Skills => skills;

    [Header("Enemy AI Skill Weights")]
    [SerializeField]
    private List<EnemySkillWeightEntry> enemySkillWeights = new List<EnemySkillWeightEntry>();

    public bool isOnField;
    public bool isActing;
    public bool isDead;
    public bool isPlayer;
    public EnemyAiType enemyAiType=EnemyAiType.Normal;
    public bool battleEnded; 
    public ActionIntent intent = ActionIntent.Normal;

   // public List<SkillData> skills;

    //public SkillData testskill;
    public void NotifyHpChange(int prev, int cur) {
        Debug.Log($"[HP EVENT]{prev}->{cur} subs={(OnHpChanged == null ? 0 : OnHpChanged.GetInvocationList().Length)}");
        OnHpChanged?.Invoke(prev, cur);
    }
    public void NotifyMpChange(int prev, int cur)
    {
        OnMpChanged?.Invoke(prev, cur);
    }
    /// <summary>
    /// 获取经验
    /// 返回本次生了多少级， 方便结算界面显示
    /// </summary>
    public int GainExp(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[GainExp]{Name} gain amount<=0:{amount}");
            return 0;
        }
        Debug.Log($"[GainExp] {Name} +{amount} EXP (before: Lv{Level}, Exp={Exp}/{GetExpToNextLevel()})");
        Exp += amount;
        int levelUpCount = 0;

        while (Exp >= GetExpToNextLevel())
        {
            Exp -= GetExpToNextLevel();
            LevelUp();
            levelUpCount++;
        }

        Debug.Log($"[GainExp] {Name} after: Lv{Level}, Exp={Exp}/{GetExpToNextLevel()}, levelUps={levelUpCount}");
        return levelUpCount;
    }
    /// <summary>
    /// 获取当前角色使用指定技能时的AI权重
    /// 没有单独配置的技能，返回默认权重1
    /// </summary>
    public int GetEnemySkillWeight(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning($"[GetEnemySkillWeight] {Name} skill is null");
            return 0;
        }
        //兼容尚未配置权重列表的旧角色数据
        if(enemySkillWeights==null)
            return 1;

      foreach(EnemySkillWeightEntry entry in enemySkillWeights)
        {
            if (entry == null || entry.Skill != skill)
                continue;
           
            return entry.Weight;
        }
      //没有特别配置时，保持所有技能等概率
        return 1;
    }

    public void LevelUp()
    {
        Level++;

        //每升一级，增加10点攻击力和5点最大HP
        MaxHp += 5;
        MaxMp += 5;
        Attack += 10;
        Speed += 0.5f;
        //HP和MP回复满
        int prevHp = Hp;
        int prevMp = Mp;

        Hp = MaxHp;
        Mp = MaxMp;

        NotifyHpChange(prevHp, Hp);
        NotifyMpChange(prevMp, Mp);

        Debug.Log($"[LevelUp] {Name} leveled up! -> Lv.{Level}");
    }
    public int GetExpToNextLevel()
    {
        return Level * 100;
    }
    //这个是为了使用copy在battle 当中的数据。
    public Character Copy()
    {
        return new Character
        {
            characterId = this.characterId,

            Team = this.Team,

            Name = this.Name,

            MaxHp = this.MaxHp,
            Hp = this.Hp,

            MaxMp = this.MaxMp,
            Mp = this.Mp,

            Level = this.Level,
            Exp = this.Exp,

            Attack = this.Attack,
            Speed = this.Speed,
            ActionValue = this.ActionValue,
            MaxActionValue = this.MaxActionValue,

            Portrait = this.Portrait,

            skills = this.skills != null ? new List<SkillData>(this.skills) : new List<SkillData>(),

            enemySkillWeights = this.enemySkillWeights != null
            ? this.enemySkillWeights.ConvertAll(entry => entry != null ? entry.Copy() : null)
            : new List<EnemySkillWeightEntry>(),

            isOnField = this.isOnField,
            isActing = this.isActing,
            isDead = this.isDead,
            isPlayer = this.isPlayer,
            enemyAiType = this.enemyAiType,
            battleEnded = this.battleEnded,
            intent = this.intent,

        };
    }
}
