using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour
{
    //这个部分是Emeny在战斗中血条的显示的单独逻辑
    [Header("血量填充部分")]
    public Image fill;
    [Header("是否跟随摄像机旋转")]
    public bool alwaysFaceCamera = true;
    
    private Camera mainCam;
    private Character owner;


    private void Start()
    {
        mainCam = Camera.main;
    }
    private void LateUpdate()
    {
        if (alwaysFaceCamera&&mainCam != null) 
        {//物体正面朝向的世界方向。
           transform.forward=mainCam.transform.forward;
        }
    }
    public void Bind(Character character)
    {
        owner = character;
        UpdateHp();
    }
    public void UpdateHp()
    {
        if (owner == null)
        {
            return;
        }
        float ratio = (float)owner.Hp / owner.maxHp;
        //该函数是unity提供的安全函数，用来把数值限制在0~1之间，
        fill.fillAmount = Mathf.Clamp01(ratio);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
