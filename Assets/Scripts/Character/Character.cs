using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Character 
{//第一天，纯数据结构
    
    public string Name;
    public int Hp;
    public int Attack;
    public float Speed;
    public float ActionValue;//行动值
    public bool isActing;
    public bool isDead;

    public Character(string name,int hp,int attack,float speed,float startValue=200f)
    {
        this.Name = name;
        this.Hp = hp;
        this.Attack = attack;
        this.Speed = speed;
        this.ActionValue = startValue;//初始行动值为200

        isActing = false;
        isDead = false;
    }
}
