using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeLineUI : MonoBehaviour
{
    [SerializeField] private BatteleManager battle;

    //cache: baseController->TimelineIconView
    private readonly Dictionary<BaseController, TimelineIconView> _map = new();

    private void Awake()
    {
        Debug.Log($"[TimelineUI] Awake activeInHierarchy={gameObject.activeInHierarchy} enabled={enabled}");
        if (battle == null) battle = BatteleManager.Instance;
        BuildCache();
        Debug.Log($"[TimelineUI] Awake battle={(battle ? battle.name : "null")} id={(battle ? battle.GetInstanceID() : -1)}");
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

            view.ForceInactive();
        }
        //战斗开始，同步当前状态
        var cur = battle.CurrentActor;
        if(cur!=null&&_map.TryGetValue(cur,out var curView)&&curView!=null)
            curView.SetActive(true);

    }
    private void HandleActionChanged(BaseController prev, BaseController cur)
    {
        Debug.Log($"[TimelineUI] ActiveChanged prev={(prev ? prev.name : "null")} cur={(cur ? cur.name : "null")} mapCount={_map.Count}");
        //prev off
        if (prev!=null&&_map.TryGetValue(prev,out var preview) && preview != null)
        {
            preview.SetActive(false);
        }
        //cur on
        if(cur!=null&&_map.TryGetValue(cur,out var curview) && curview != null)
        {
            curview.SetActive(true);
        }
        else if (cur != null)
            Debug.LogWarning($"[TimelineUI] cur not in map: {cur.name}");
    }
    private void TryBindBattle()
    {
        if (battle == null)
            battle = BatteleManager.Instance;

        if (battle != null)
        {
            battle.OnActionChanged -= HandleActionChanged;
            battle.OnActionChanged += HandleActionChanged;
        }
    }
    private void UnBindBattle()
    {
        if (battle != null)
        {
            battle.OnActionChanged -= HandleActionChanged;
        }
    }

}
