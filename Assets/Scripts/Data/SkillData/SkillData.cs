using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Battle/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;
    public SkillTargetType targetType;
    public int mpCost;
    public int power;

    [Header("Damage Over Time")]
    [Min(1)]
    [Tooltip("For Dot skills, power is damage per tick. Ticks happen at the target's turn end.")]
    public int dotDurationTurns = 2;

    [Header("Enemy AI")]
    [Min(0)]
    public int enemyCooldownTurns;
}
public enum SkillType
{
    Damage = 0,
    Heal = 1,
    Revive = 2,
    Dot = 3
}
