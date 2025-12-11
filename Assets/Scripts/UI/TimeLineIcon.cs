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
        if (owner == null || panelRect == null || rectIcon == null) return;

        // 确保有合理的maxActionValue
        float maxValue = owner.data.maxActionValue > 0 ? owner.data.maxActionValue : 200f;

        // 限制行动值范围
        float actionValue = Mathf.Clamp(owner.data.ActionValue, 0f, maxValue);

        // 计算位置比例 (0在底部，1在顶部)
        // 注意：这里应该是ActionValue越高（即将行动），图标应该在顶部
        float t = actionValue / maxValue;

        // 反转：即将行动的（值小）在顶部，值大的在底部
        t = 1f - t;

        // 计算Y位置 (面板底部到顶部)
        float minY = -barHeight / 2f + rectIcon.rect.height / 2f;
        float maxY = barHeight / 2f - rectIcon.rect.height / 2f;

        float yPos = Mathf.Lerp(minY, maxY, t);

        rectIcon.anchoredPosition = new Vector2(rectIcon.anchoredPosition.x, yPos);

        Debug.Log($"{owner.data.Name}: AV={owner.data.ActionValue}, t={t}, yPos={yPos}");

    }
}
