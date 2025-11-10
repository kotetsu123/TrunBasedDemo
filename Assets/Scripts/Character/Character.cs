using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public int Hp;
    public int Attack;
    public float Speed;
    public float ActionValue;//行动值
    public bool isActing;
    public bool isDead;
    public bool isPlayer;

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
    //TODO：选中入口
    public void AttackTarget(Character target)//之后特效，音效等 选中入口 伤害判定由TakeDamage处理
    {
        target.Hp -= Attack;
        if (target.Hp <= 0)
        {
            target.Hp = 0;
            target.isDead = true;
            Debug.Log(target.Name + " is dead!");
        }
        else
        {
             Debug.Log($"{Name} attacks {target.Name}, make {Attack} damege!");
        }      
    }
    public void TakeDamage(int damage)
    {
               Hp -= damage;
        if (Hp <= 0)
        {
            Hp = 0;
            isDead = true;
            Debug.Log($"{this.Name} +  is dead!");
            
        }
        else
        {
            Debug.Log($"{this.Name} takes {damage} damage, remaining HP: {Hp}");
        }
    }
}
