using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TimeLineIcon : MonoBehaviour
{
    /* public Image protrait;//头像
     public Image actionFIll;//数条fill*/
    // public RectTransform iconRect;

    private RectTransform panelRect;
    
    private RectTransform rectIcon;
    private BaseController owner;//对应角色
    private float barHeight ;//ActionbarPanel 的高度


    private void Awake()
    {
       //panelRect=GetComponent<RectTransform>();
        //rectIcon = GetComponent<RectTransform>();//图标自己
        //barHeight = panelRect.rect.height;
        //Debug.Log("barHeight=" + barHeight);
    }
    public void Bind(BaseController character,RectTransform parentPanel)
    {
        owner = character;
        panelRect = parentPanel;//时间条面板
        rectIcon = GetComponent<RectTransform>();

        //rectIcon.SetParent(panelRect, false);//设置父物体

        barHeight = panelRect.rect.height;

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
     public  void UpdatePosition()
    {//inverseLerp(起点，终点，比例)  那也就是说从下往上的时间轴来说确实是100f-0f
        if (owner == null) return;

        /*  float t = Mathf.InverseLerp(100f, 0f, owner.data.ActionValue);
          float yPos = Mathf.Lerp(0f, barHeight, t);

          rect.anchoredPosition = new Vector2(0, yPos);*/
        /* float t = Mathf.InverseLerp(owner.data.startValue, 0f, owner.data.ActionValue);
         //计算最终位置
         float yPos = Mathf.Lerp(0f, barHeight, t);
         //防止越界
         float iconHalf = rectIcon.rect.height * 0.5f;
         yPos = Mathf.Clamp(yPos, iconHalf, barHeight-iconHalf);
         rectIcon.anchoredPosition=new Vector2 (rectIcon.anchoredPosition.x, yPos);
 */
        /*  float ratio = 1f - Mathf.Clamp01(owner.data.ActionValue / 200f);
          float yPos=ratio*barHeight;
          rectIcon.anchoredPosition=new Vector2(rectIcon.anchoredPosition.x,yPos);*/
        /* float iconHalf = rectIcon.rect.height * 0.5f;
        //yPos = Mathf.Clamp(yPos, iconHalf, barHeight - iconHalf);
         // 先限制范围，避免负值造成位置越界
         float av = Mathf.Clamp(owner.data.ActionValue, 0f, owner.data.maxActionValue);

         // 映射到 0 ~ 1
         float t = 1f - (av / owner.data.maxActionValue);

         // 转换成最终的 Y 坐标
         float yPos = Mathf.Lerp(0f, barHeight, t);
          yPos = Mathf.Clamp(yPos, iconHalf, barHeight - iconHalf);

         // 设置位置
         rectIcon.anchoredPosition = new Vector2(rectIcon.anchoredPosition.x, yPos);*/
        if (owner == null) return;

        float av = Mathf.Clamp(owner.data.ActionValue, 0f, owner.data.maxActionValue);
        float t = 1f - (av / owner.data.maxActionValue);

        // 注意 barHeight 要减去图标高度
        float maxY = barHeight - rectIcon.rect.height;
        float yPos = Mathf.Lerp(0f, maxY, t);

        rectIcon.anchoredPosition = new Vector2(rectIcon.anchoredPosition.x, yPos);
        Debug.Log($"bar={barHeight}");
        Debug.Log($"value={owner.data.ActionValue}，{owner.data.Name}");
        Debug.Log($"pos={yPos}");


    }
}
