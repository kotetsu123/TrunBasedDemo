using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


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
    private TMP_Text intentText;
    private TMP_Text dotText;
    private float barHeight ;//ActionbarPanel 的高度


    private void Awake()
    {
        rect= GetComponentInParent<RectTransform>();
        barHeight = rect.rect.height;
    }
    public void Bind(BaseController character)
    {
        if (owner != null) owner.OnDotChanged -= RefreshDot;
        owner = character;
        if (owner != null) owner.OnDotChanged += RefreshDot;
        if (portImage!=null)
        {
            // Character data is the main portrait source. The controller/prefab portrait only stays as a fallback.
            portImage.sprite = owner.data.Portrait != null ? owner.data.Portrait : owner.portrait;
            portImage.enabled = portImage.sprite != null;
        }
        RefreshDot();
    }
   public void SetHighLight(bool on)
    {
        if(highlight != null)
        {
            highlight.SetActive(on);
        }
    }

    public void SetIntent(string message)
    {
        if (intentText == null && !string.IsNullOrEmpty(message))
        {
            var label = new GameObject("Intent", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(1f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(8f, 0f);
            labelRect.sizeDelta = new Vector2(190f, 24f);
            intentText = label.GetComponent<TMP_Text>();
            intentText.fontSize = 16f;
            intentText.color = new Color(1f, 0.75f, 0.25f);
            intentText.alignment = TextAlignmentOptions.Left;
            intentText.enableWordWrapping = false;
            intentText.enableAutoSizing = true;
            intentText.fontSizeMin = 10f;
            intentText.fontSizeMax = 16f;
            intentText.raycastTarget = false;
        }
        if (intentText == null) return;
        intentText.text = message ?? string.Empty;
        intentText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private void RefreshDot()
    {
        bool active = owner != null && owner.DotTurnsRemaining > 0;
        if (dotText == null && active)
        {
            var label = new GameObject("DotStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = new Vector2(1f, 0.5f);
            labelRect.anchorMax = new Vector2(1f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(8f, -22f);
            labelRect.sizeDelta = new Vector2(190f, 18f);
            dotText = label.GetComponent<TMP_Text>();
            dotText.color = new Color(1f, 0.45f, 0.3f);
            dotText.alignment = TextAlignmentOptions.Left;
            dotText.fontSize = 14f;
            dotText.enableWordWrapping = false;
            dotText.enableAutoSizing = true;
            dotText.fontSizeMin = 10f;
            dotText.fontSizeMax = 14f;
            dotText.raycastTarget = false;
        }
        if (dotText == null) return;
        dotText.text = active ? $"{owner.DotName} ({owner.DotTurnsRemaining})" : string.Empty;
        dotText.gameObject.SetActive(active);
    }

    private void OnDestroy()
    {
        if (owner != null) owner.OnDotChanged -= RefreshDot;
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
