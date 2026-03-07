using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TimeLineIcon : MonoBehaviour
{
    /* public Image protrait;//头像
     public Image actionFIll;//数条fill*/
    // public RectTransform iconRect;
    [Header("UI")]
    [SerializeField]
    private Image portImage;//头像
    [SerializeField]
    private GameObject highlight;


    private RectTransform rect;
    private BaseController owner;//对应角色
    private float barHeight ;//ActionbarPanel 的高度


    private void Awake()
    {
        rect= GetComponentInParent<RectTransform>();
        barHeight = rect.rect.height;
    }
    public void Bind(BaseController character)
    {
        owner = character;
        if (portImage!=null)
        {
            portImage.sprite = owner.portrait;
        }
    }
   public void SetHighLight(bool on)
    {
        if(highlight != null)
        {
            highlight.SetActive(on);
        }
    }

    // Update is called once per frame
    void Update()
    {
       /* if (owner == null) return;
        //1.更新整条fill//TODO: 搞明白为什么写了“/200f”
        float ratio = Mathf.Clamp01(owner.data.ActionValue);
        actionFIll.fillAmount = ratio;

        //2.根据actionValue 在父面板当中移动
        float normalized=1f - ratio;//0行动条顶部 1为底部
        float newY = normalized * barHeight - barHeight / 2f;

        rect.anchoredPosition= new Vector2(rect.anchoredPosition.x, newY);*/
    }
    void UpdatePosition()
    {
        float t = Mathf.InverseLerp(100f, 0f, owner.data.ActionValue);
        float yPos = Mathf.Lerp(0f, barHeight, t);

        rect.anchoredPosition = new Vector2(0, yPos);
    }
    
}
