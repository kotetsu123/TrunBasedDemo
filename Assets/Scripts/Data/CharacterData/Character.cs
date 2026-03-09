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
    public int Exp = 0;//总经验

    public int Attack;
    public float Speed;
    public float ActionValue;//行动值
    public float MaxActionValue = 200f;//最大行动值//初始行动值

    public bool isOnField;
    public bool isActing;
    public bool isDead;
    public bool isPlayer;
    public bool battleEnded; public ActionIntent intent = ActionIntent.Normal;

    public List<SkillData> skills;

    public SkillData testskill;
    public void NotifyHpChange(int prev, int cur) {
        Debug.Log($"[HP EVENT]{prev}->{cur} subs={(OnHpChanged == null ? 0 : OnHpChanged.GetInvocationList().Length)}");
        OnHpChanged?.Invoke(prev, cur);
    }
    public void NotifyMpChange(int prev, int cur)
    {
        OnMpChanged?.Invoke(prev, cur);
    }
    public int GetExpToNextLevel()
    {
        return Level * 100;
    }

}
