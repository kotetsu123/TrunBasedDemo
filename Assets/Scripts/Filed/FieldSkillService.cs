using System.Collections.Generic;
using UnityEngine;

public static class FieldSkillService
{
    private static bool applying;

    public static bool IsFieldSkill(SkillData skill)
    {
        return skill != null && ((skill.skillType == SkillType.Heal &&
            (skill.targetType == SkillTargetType.Self || skill.targetType == SkillTargetType.AllySingle || skill.targetType == SkillTargetType.AllyAll))
            || (skill.skillType == SkillType.Revive && skill.targetType == SkillTargetType.AllyDeadSingle));
    }

    public static bool OwnsSkill(Character caster, SkillData skill)
    {
        if (caster?.Skills == null || skill == null) return false;
        foreach (var known in caster.Skills) if (known == skill) return true;
        return false;
    }

    public static string UnavailableReason(Character caster, SkillData skill)
    {
        if (skill == null) return "No skill selected.";
        if (!IsFieldSkill(skill)) return "Battle only.";
        if (!IsPartyMember(caster)) return "No character selected.";
        if (!OwnsSkill(caster, skill)) return "Skill not learned.";
        if (caster.isDead || caster.Hp <= 0) return "Cannot cast while KO.";
        if (skill.mpCost < 0 || skill.power <= 0) return "Invalid skill configuration.";
        if (caster.Mp < skill.mpCost) return "Not enough MP.";
        foreach (var target in PartyRuntimeState.PartyMembers)
            if (CanTarget(caster, skill, target)) return string.Empty;
        if (skill.targetType == SkillTargetType.Self) return "Already at full HP.";
        return skill.skillType == SkillType.Revive ? "No fallen allies." : "No injured allies.";
    }

    public static bool CanTarget(Character caster, SkillData skill, Character target)
    {
        if (!IsFieldSkill(skill) || !IsPartyMember(caster) || !IsPartyMember(target)
            || skill.power <= 0 || target.MaxHp <= 0) return false;
        if (skill.skillType == SkillType.Revive) return target.isDead || target.Hp <= 0;
        return !target.isDead && target.Hp > 0 && target.Hp < target.MaxHp
            && (skill.targetType != SkillTargetType.Self || ReferenceEquals(caster, target));
    }

    public static bool TryUse(Character caster, SkillData skill, Character selectedTarget, out string message)
    {
        if (applying) { message = "Skill in progress."; return false; }
        message = UnavailableReason(caster, skill);
        if (message.Length > 0) return false;
        var targets = new List<Character>();
        if (skill.targetType == SkillTargetType.AllyAll)
        {
            foreach (var member in PartyRuntimeState.PartyMembers)
                if (CanTarget(caster, skill, member) && !targets.Contains(member)) targets.Add(member);
        }
        else
        {
            var target = skill.targetType == SkillTargetType.Self ? caster : selectedTarget;
            if (!CanTarget(caster, skill, target)) { message = "Choose a valid target."; return false; }
            targets.Add(target);
        }

        // Commit the complete cast before notifying UI, including a group cast's single MP cost.
        applying = true;
        try
        {
            int previousMp = caster.Mp;
            var previousHp = new int[targets.Count];
            caster.Mp -= skill.mpCost;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                previousHp[i] = target.Hp;
                if (skill.skillType == SkillType.Revive)
                {
                    target.isDead = false;
                    target.Hp = Mathf.Clamp(skill.power, 1, target.MaxHp);
                }
                else target.Hp = (int)System.Math.Min((long)target.Hp + skill.power, target.MaxHp);
            }
            caster.NotifyMpChange(previousMp, caster.Mp);
            for (int i = 0; i < targets.Count; i++) targets[i].NotifyHpChange(previousHp[i], targets[i].Hp);
            message = skill.skillName + " used.";
            return true;
        }
        finally { applying = false; }
    }

    public static string TargetLabel(SkillData skill)
    {
        if (skill == null) return string.Empty;
        switch (skill.targetType)
        {
            case SkillTargetType.Self: return "Self";
            case SkillTargetType.AllySingle: return "One ally";
            case SkillTargetType.AllyAll: return "All living allies";
            case SkillTargetType.AllyDeadSingle: return "One fallen ally";
            case SkillTargetType.EnemyAll: return "All enemies";
            default: return "One enemy";
        }
    }

    public static string Description(Character caster, SkillData skill)
    {
        if (skill == null) return string.Empty;
        switch (skill.skillType)
        {
            case SkillType.Heal: return $"Restore up to {skill.power} HP.";
            case SkillType.Revive: return $"Revive a fallen ally with {skill.power} HP.";
            case SkillType.ApplyDebuff: return $"Deal {skill.power} damage at the end of each affected unit's action for {skill.debuffDurationTurns} actions.";
            default: return $"Deal Attack + {skill.power} damage.";
        }
    }

    private static bool IsPartyMember(Character member)
    {
        if (member == null) return false;
        foreach (var current in PartyRuntimeState.PartyMembers) if (ReferenceEquals(current, member)) return true;
        return false;
    }
}
