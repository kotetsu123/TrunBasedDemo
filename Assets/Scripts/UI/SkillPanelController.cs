using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillPanelController : BasePanel
{
    public event Action<SkillData> OnSkillSelected;
    public event Action OnCancel;

    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillItemView skillItemPrefab;

    private readonly List<SkillItemView> _items = new();
    private BaseController _actor;

  protected override void Awake()
    {
        base.Awake();
        HideImmediate();
    }
    public void Show(BaseController actor)
    {
        Rebuild(actor);

        base.Show();
    }
    public void OnClickSkill(SkillData skill)
    {
        var battle = BattleManager.Instance;
        if (battle == null || !battle.CanSelectSkill(_actor, skill))
            return;

        OnSkillSelected?.Invoke(skill);
    }
    public void OnClickBack()
    {
        OnCancel?.Invoke();
    }
    public void Rebuild(BaseController actor)
    {
        _actor = actor;
        ClearItems();
        if (actor == null || actor.data == null) return;

        Debug.Log($"[SkillPanelRebuild] actor name is {actor.name}");
        var battle = BattleManager.Instance;

        Debug.Log($"[SkillPanel] skill count = {actor.Skills.Count}");
        foreach (var skill in actor.Skills)
        {
            if (skill == null) continue;

            Debug.Log($"[SkillPanel] create item for {skill.skillName}");
            var item = Instantiate(skillItemPrefab, contentRoot);
            bool canSelect = battle != null && battle.CanSelectSkill(actor, skill);
            item.Bind(skill, actor.data.Mp, OnClickSkill, canSelect);
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
