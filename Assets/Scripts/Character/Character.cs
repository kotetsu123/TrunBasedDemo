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
    public bool isActing;
    public bool isDead;
    public bool isPlayer;
    public bool battleEnded;

   
}
