using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TimelineIconView;

public class TimeLineUI : MonoBehaviour
{
    [SerializeField] private BatteleManager battle;

    private BaseController _lastActive;
    private BaseController _lastNext;

    //cache: baseController->TimelineIconView
    private readonly Dictionary<BaseController, TimelineIconView> _map = new();

    private void Awake()
    {
        Debug.Log($"[TimelineUI] Awake activeInHierarchy={gameObject.activeInHierarchy} enabled={enabled}");
        if (battle == null) battle = BatteleManager.Instance;
        BuildCache();
        Debug.Log($"[TimelineUI] Awake battle={(battle ? battle.name : "null")} id={(battle ? battle.GetInstanceID() : -1)}");
        //Debug.Log($"[TimelineUI] instance{name} id={GetInstanceID()}");
    }
    private void OnEnable()
    {
        TryBindBattle();
    }
    
    private void OnDisable()
    {
        UnBindBattle();
    }
    private void Start()
    {
        TryBindBattle();
    }
    //如果在战斗中动态注册角色，需要在注册后调用一次BuildCache或单独AddToCache

    public void BuildCache()
    {
        //清理旧缓存
        //也就是全部重置状态，
        //这个方法的主要作用是。建立缓存+重置
        _map.Clear();
        if (battle == null) return;

        foreach(var kv in battle.TimeLineIcons)
        {
            var controller = kv.Key;
            var icon = kv.Value;
            if (controller == null || icon == null) continue;

            var view=icon.GetComponent<TimelineIconView>();
            if (view == null)
            {
                Debug.LogWarning($"[TimelineUI] TimeLineIcon missing TimelineIconView on {icon.name}");
                continue;
            }
            _map[controller] = view;

            view.ForceInactive(TimelineIconView.TimeLineState.Normal);
        }
        //战斗开始，同步当前状态
        var cur = battle.CurrentActor;
        if(cur!=null&&_map.TryGetValue(cur,out var curView)&&curView!=null)
            curView.SetActive(true);

    }
    private void HandleActionChanged(BaseController prev, BaseController cur)
    {

        //1）关掉上一个Active
        if (_lastActive != null && _map.TryGetValue(_lastActive, out var prevView) && prevView != null)
            prevView.SetState(TimelineIconView.TimeLineState.Normal, Color.white);

        /*//2）关掉上一个Next
        if(_lastNext != null && _map.TryGetValue(_lastNext, out var nextViewOld) && nextViewOld != null)
            nextViewOld.SetState(TimelineIconView.TimeLineState.Normal, Color.white);*/

        //3）当前行动者设置为Active
        if(cur!=null&&_map.TryGetValue(cur,out var curView)&&curView!=null)
            curView.SetState(TimelineIconView.TimeLineState.Active, Color.white);

       /* //4）计算next 从battle.controllers 排序拿第二个
        var next=GetNextActor(cur);

        if(next!=null&&_map.TryGetValue(next,out var nextView)&&nextView!=null)
            nextView.SetState(TimelineIconView.TimeLineState.Next, GetNextGlowColor(next));*/
       _lastActive = cur;

        Debug.Log($"[TimelineUI] ActiveChanged prev={(prev ? prev.name : "null")} cur={(cur ? cur.name : "null")} mapCount={_map.Count}");
       /* //prev off
        if (prev != null && _map.TryGetValue(prev, out var preview) && preview != null)
        {
            preview.SetActive(false);
        }
        //cur on
        if (cur != null && _map.TryGetValue(cur, out var curview) && curview != null)
        {
            curview.SetActive(true);
        }
        else if (cur != null)
            Debug.LogWarning($"[TimelineUI] cur not in map: {cur.name}");*/
    }

    private BaseController GetNextActor(BaseController current)
    {
        if (battle == null) return null;

        var ordered=battle.controllers
            .Where(c=>c!=null&&!c.data.isDead)
            .OrderBy(c=>c.data.ActionValue)
            .ToList();

        //current 可能就是ordered[0] , next 就找第一个！=current的

        return ordered.FirstOrDefault(c => c != current);
    }

    private Color GetNextGlowColor(BaseController next)
    {
        /*
        //TODO: 以后根据放技能/危险程度等调整颜色（有点太大了）
        //平A和危险技能改色
        //先阵营区分：敌人红，友军蓝
        if(next==null||next.data==null)return Color.white;

        //危险技能：敌人更红，友军更蓝
        if(next.data.intent==ActionIntent.SkillDanerous)
            return next.isPlayer ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.2f, 0.2f);*/

        //普通情况，按照阵营提示
        return next.isPlayer ?Color.cyan: Color.red;
    }
    private void TryBindBattle()
    {
        if (battle == null)
            battle = BatteleManager.Instance;

        if (battle != null)
        {
            battle.OnActionChanged -= HandleActionChanged;
            battle.OnActionChanged += HandleActionChanged;

            battle.OnTimeLineOrdered -= HandleTimeLineOrdered;
            battle.OnTimeLineOrdered += HandleTimeLineOrdered;

        }
    }

    private void HandleTimeLineOrdered(List<BaseController> ordered)
    {
        if (ordered == null || ordered.Count == 0) return;
        if (_map.Count == 0) BuildCache();

        var active = battle.CurrentActor; // 当前行动者（可能为 null）
        var next = ordered.FirstOrDefault(c => c != null && !c.data.isDead && c != active);

        // 清旧 next
        if (_lastNext != null && _lastNext!=next &&_map.TryGetValue(_lastNext, out var oldNextView) && oldNextView != null)
            oldNextView.SetState(TimeLineState.Normal, Color.white);

        // 设新 next（只要不是 active）
        if (next != null && _map.TryGetValue(next, out var nextView) && nextView != null)
            //nextView.SetState(TimeLineState.Next, next.isPlayer ? Color.blue : Color.red);
            nextView.SetState(TimeLineState.Next, GetNextGlowColor(next));


        _lastNext = next;
        
    }

    private void UnBindBattle()
    {
        if (battle != null)
        {
            battle.OnActionChanged -= HandleActionChanged;
        }
    }

}
