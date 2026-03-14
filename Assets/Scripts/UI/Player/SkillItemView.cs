using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;


public class SkillItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text skillNameTxt;
    [SerializeField] private TMP_Text mpCostTxt;

    private SkillData _skill;
    private Action<SkillData> _onClick;

    public void Bind(SkillData skill,int currentMp,Action<SkillData> onClick)
    {
        _skill = skill;
        _onClick = onClick;

        skillNameTxt.text=skill.skillName;
        mpCostTxt.text = $"MP{skill.mpCost}";

        bool canUse=currentMp>=skill.mpCost;
        button.interactable = canUse;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }
    private void HandleClick()
    {
        _onClick?.Invoke(_skill);
    }
}
