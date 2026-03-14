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
}
public enum SkillType
{
    Damage,
    Heal,
    Revive
}
