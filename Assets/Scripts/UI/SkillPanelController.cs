using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillPanelController : MonoBehaviour
{

    public event Action<SkillData> OnSkillSelected;
    public event Action OnCancel;

   
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillItemView skillItemPrefab;

    private readonly List<SkillItemView> _items = new();

    public bool IsOpen => canvasGroup != null && canvasGroup.alpha > 0.99f;
    private void Awake()
    {
        HideImmediate();
    }
    public void Show(BaseController actor)
    {
        Rebuild(actor);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public  void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void OnClickSkill(SkillData skill)
    {
        OnSkillSelected?.Invoke(skill);
    }
    public void OnClickBack()
    {
        OnCancel?.Invoke();
    }
    public void Rebuild(BaseController actor)
    {
        Debug.Log($"[SkillPanelRebuild] actor name is {actor.name}");
        ClearItems();
        if (actor == null) return;

        Debug.Log($"[SkillPanel] skill count = {actor.Skills.Count}");
        foreach (var skill in actor.Skills)
        {
            Debug.Log($"[SkillPanel] create item for {skill.skillName}");
            var item = Instantiate(skillItemPrefab, contentRoot);
            item.Bind(skill, actor.data.Mp, OnClickSkill);
            _items.Add(item);
        }
    }
    private void ClearItems()
    {
        foreach (var item in _items)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _items.Clear();
    }
    
}
