using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Battle/Skill")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public int mpCost;
    public int power;

}
