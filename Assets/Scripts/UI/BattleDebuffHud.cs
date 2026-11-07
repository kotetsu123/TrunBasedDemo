using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleDebuffHud : MonoBehaviour
{
    [SerializeField] private SkillData burnSkill;
    [SerializeField] private Sprite burnIcon;
    [SerializeField] private Vector2 anchor = Vector2.zero;
    [SerializeField] private Vector2 offset = new Vector2(105f, -4f);
    [SerializeField, Min(12f)] private float iconSize = 26f;

    private BaseController _owner;
    private bool _bound;
    private GameObject _iconRoot;
    private TMP_Text _turns;

    public void Bind(BaseController owner)
    {
        if (this == null) return;
        Unsubscribe();
        _owner = owner;
        if (isActiveAndEnabled) Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (_bound || _owner == null) return;
        _owner.OnDebuffChanged += Refresh;
        _bound = true;
    }

    private void Unsubscribe()
    {
        if (_bound && _owner != null) _owner.OnDebuffChanged -= Refresh;
        _bound = false;
    }

    private void Refresh()
    {
        if (this == null) return;
        bool visible = isActiveAndEnabled && _owner != null && _owner.data != null && !_owner.isDead && _owner.data.Hp > 0
            && _owner.ActiveDebuff.IsActive && burnSkill != null && burnIcon != null
            && _owner.ActiveDebuff.SourceSkill == burnSkill;
        if (_iconRoot == null && visible) BuildIcon();
        if (_iconRoot == null) return;
        _iconRoot.SetActive(visible);
        if (visible) _turns.text = _owner.ActiveDebuff.RemainingTurns.ToString();
    }

    private void BuildIcon()
    {
        _iconRoot = new GameObject("BurnStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _iconRoot.layer = gameObject.layer;
        var rect = (RectTransform)_iconRoot.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = Vector2.one * iconSize;
        var image = _iconRoot.GetComponent<Image>();
        image.sprite = burnIcon;
        image.preserveAspect = true;
        image.raycastTarget = false;

        var label = new GameObject("Turns", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.layer = gameObject.layer;
        var labelRect = (RectTransform)label.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(1f, 0f);
        labelRect.sizeDelta = new Vector2(Mathf.Max(18f, iconSize * 0.75f), iconSize * 0.7f);
        _turns = label.GetComponent<TMP_Text>();
        _turns.fontSize = iconSize * 0.55f;
        _turns.enableAutoSizing = true;
        _turns.fontSizeMin = 8f;
        _turns.fontSizeMax = iconSize * 0.55f;
        _turns.fontStyle = FontStyles.Bold;
        _turns.alignment = TextAlignmentOptions.BottomLeft;
        _turns.color = Color.white;
        _turns.outlineColor = Color.black;
        _turns.outlineWidth = 0.2f;
        _turns.enableWordWrapping = false;
        _turns.raycastTarget = false;
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_iconRoot != null) _iconRoot.SetActive(false);
    }
    private void OnDestroy() => Unsubscribe();
}
