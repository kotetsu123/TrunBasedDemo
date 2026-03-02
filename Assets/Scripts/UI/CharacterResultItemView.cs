using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterResultItemView : MonoBehaviour
{
    [SerializeField] private Image portrait;

    [Header("HP")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpTxt;

    [Header("MP")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private TMP_Text mpTxt;

    [Header("Level/Exp")]
    [SerializeField] private TMP_Text levelTxt;
    [SerializeField] private TMP_Text expTxt;//expToNext 

    public void Bind(CharacterResultSnapshot s)
    {
        if (portrait) portrait.sprite = s.portrait;

        hpSlider.maxValue = s.maxhp;
        hpSlider.value=s.hp;
        hpTxt.text = $"{s.hp}/{s.maxhp}";

        mpSlider.maxValue = s.mp;
        mpSlider.value = s.mp;
        mpTxt.text = $"{s.mp}/{s.maxmp}";

        levelTxt.text = $"LV.{s.level}";
        expTxt.text = $"--";
    }
}
