using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

[Serializable]
public class Character

{//第一天，纯数据结构
    public enum TeamType
    {
        Player,
        Enemy
    }
    public TeamType Team;

    public string Name;
    public int maxHp;
    public int Hp;
    public int Attack;
    public float Speed;
    public float ActionValue;//行动值
    public float startValue=200f;//角色行动操的初始容量
    public float maxActionValue = 200f;
    public bool isActing;
    public bool isDead;
    public bool isPlayer;
    public bool battleEnded;

    //数据类构造函数
    //暂时不启用 后续打算做批量时启用
   /* public Character(string name,int maxHp,int attack,float speed,float startValue = 100f)
    {
        this.Name = name;
        this.maxHp = maxHp;
        this.Hp = maxHp;
        this.Attack = attack;
        this.Speed = speed;
        this.ActionValue=startValue;
    }*/
}
