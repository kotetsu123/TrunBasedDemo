using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName ="Battle/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;
    public SkillTargetType targetType;
    public int mpCost;
    public int power;

    [Header("Debuff")]
    public DebuffEffectType debuffEffect = DebuffEffectType.DamageOverTime;
    [FormerlySerializedAs("dotDurationTurns")]
    [Min(1)]
    [Tooltip("Duration in the affected unit's own completed actions.")]
    public int debuffDurationTurns = 2;

    public bool HasValidDebuff => skillType == SkillType.ApplyDebuff
        && debuffEffect == DebuffEffectType.DamageOverTime
        && power > 0 && debuffDurationTurns > 0;

    [Header("Enemy AI")]
    [Min(0)]
    public int enemyCooldownTurns;
}
public enum SkillType
{
    Damage = 0,
    Heal = 1,
    Revive = 2,
    ApplyDebuff = 3
}

public enum DebuffEffectType
{
    DamageOverTime = 0
}
