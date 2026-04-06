using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;


[Serializable]
public class Character

{
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

    public bool isOnField;
    public bool isActing;
    public bool isDead;
    public bool isPlayer;
    public bool battleEnded; public ActionIntent intent = ActionIntent.Normal;

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

}
