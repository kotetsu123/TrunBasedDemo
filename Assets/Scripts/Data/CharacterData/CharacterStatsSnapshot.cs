using System;
using UnityEngine;

public sealed class CharacterStatsSnapshot
{
    public string CharacterId { get; }
    public string Name { get; }
    public Sprite Portrait { get; }
    public int Level { get; }
    public int Exp { get; }
    public int ExpToNextLevel { get; }
    public int Hp { get; }
    public int MaxHp { get; }
    public int Mp { get; }
    public int MaxMp { get; }
    public int Attack { get; }
    public float Speed { get; }
    public bool IsDead { get; }

    public CharacterStatsSnapshot(Character member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        CharacterId = member.characterId;
        Name = member.Name;
        Portrait = member.Portrait;
        Level = member.Level;
        Exp = member.Exp;
        ExpToNextLevel = member.GetExpToNextLevel();
        Hp = member.Hp;
        MaxHp = member.MaxHp;
        Mp = member.Mp;
        MaxMp = member.MaxMp;
        Attack = member.Attack;
        Speed = member.Speed;
        IsDead = member.isDead || member.Hp <= 0;
    }
}
