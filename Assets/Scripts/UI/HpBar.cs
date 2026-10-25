using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HpBar : MonoBehaviour
{
    //这个部分是Emeny在战斗中血条的显示的单独逻辑
    [Header("血量填充部分")]
    public Image fill;
    [Header("是否跟随摄像机旋转")]
    public bool alwaysFaceCamera = true;

    public bool isWorldSpace = true;
    
    private Camera mainCam;
    private BaseController owner;
    private TMP_Text intentText;


    private void Start()
    {
        mainCam = Camera.main;
        
    }
    private void LateUpdate()
    {
        if (alwaysFaceCamera&&mainCam != null&&isWorldSpace==true) 
        {//物体正面朝向的世界方向。
           transform.forward=mainCam.transform.forward;
        }
    }
    public void Bind(BaseController character)
    {
        UnBind();

        owner = character;
        if (owner != null&&owner.data!=null) {
            owner.data.OnHpChanged += HandleHpChanged;
        }
        UpdateHp();
       /* if (owner != null)
        {
            Debug.Log($"{owner.data.Name} 捆绑的owener的名字");
        }*/
    }
    private void HandleHpChanged(int prevHp,int curHp)
    {
        UpdateHp();
    }
    public void UpdateHp()
    {
        if (owner == null)
        {
            return;
        }
        float ratio = (float)owner.data.Hp / owner.data.MaxHp;
        //该函数是unity提供的安全函数，用来把数值限制在0~1之间，
        fill.fillAmount = Mathf.Clamp01(ratio);
    }
    private void UnBind()
    {
        if (owner != null && owner.data != null)
        {
            owner.data.OnHpChanged -= HandleHpChanged;
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void SetIntent(string message)
    {
        if (intentText == null && !string.IsNullOrEmpty(message))
        {
            var label = new GameObject("Intent", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(transform, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);
            rect.sizeDelta = new Vector2(240f, 24f);

            intentText = label.GetComponent<TMP_Text>();
            intentText.fontSize = 16f;
            intentText.alignment = TextAlignmentOptions.Center;
            intentText.color = new Color(1f, 0.75f, 0.25f);
            intentText.enableWordWrapping = false;
            intentText.enableAutoSizing = true;
            intentText.fontSizeMin = 10f;
            intentText.fontSizeMax = 16f;
            intentText.raycastTarget = false;
        }

        if (intentText == null)
            return;

        intentText.text = message ?? string.Empty;
        intentText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
    private void OnDestroy()
    {
        UnBind();
    }
}
