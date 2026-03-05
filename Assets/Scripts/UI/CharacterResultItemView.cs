using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
//using DG.Tweening;

public class CharacterResultItemView : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text Name;
    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpTxt;

    [Header("MP")]
    [SerializeField] private Image mpFill;
    [SerializeField] private TMP_Text mpTxt;

    [Header("Level/Exp")]
    [SerializeField] private TMP_Text levelTxt;
    [SerializeField] private TMP_Text expTxt;//expToNext 

    public void Bind(CharacterResultSnapshot s)
    {
        if (portrait) portrait.sprite = s.portrait;
        name = s.Name;

        hpFill.fillAmount = (float)s.hp / s.maxhp;
        //hpFill.DOFillAmount((float)s.hp/s.maxhp,0.3f);      
        hpTxt.text = $"{s.hp}/{s.maxhp}";

        mpFill.fillAmount =(float)s.mp / s.maxmp;
       // mpFill.DOFillAmount((float)s.mp / s.maxmp, 0.3f);
        mpTxt.text = $"{s.mp}/{s.maxmp}";

        levelTxt.text = $"LV.{s.level}";
        expTxt.text = $"--";
    }
}
