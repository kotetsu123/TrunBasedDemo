using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BatteleManager : MonoBehaviour
{
    public static BatteleManager Instance{ get; private set; }
    //c#event 广播，当行动者改变时触发
    public event Action<BaseController, BaseController> OnActionChanged;
    public event Action<List<BaseController>> OnTimeLineOrdered;

   // public List<Character> characters = new List<BaseController>();
    public List<BaseController> controllers = new List<BaseController>();

    public bool isActing = false;
    public bool battleEnded = false;
    public bool isBattle = true;
    public bool isBattleReady = false;

    public PlayerController player;
    public EnemyController enemy;
    public GameObject timeLineIconPrefab;
    public RectTransform actionBarPanel;

    public List<RectTransform> slots;

    private Dictionary<BaseController, TimeLineIcon> timeLineIcons = new Dictionary<BaseController, TimeLineIcon>();
    public IReadOnlyDictionary<BaseController,TimeLineIcon>TimeLineIcons=>timeLineIcons;
    private int tick = 0;//时间刻度

    //当前行动者
    private BaseController _currentActor;
    public BaseController CurrentActor=>_currentActor;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //StartCoroutine(RigisterDone());
    }

    void Update()
    {
        if (isActing) return;//正在行动中，跳过本次更新
        if (!isBattleReady) return;//战斗未准备好，跳过本次更新

        tick++;
        //每隔10个时间刻度，所有角色增加行动值
        if (tick % 10 == 0)
        {
            updateActionValues();
        }
        if (battleEnded)
        {
            return;
        }         
    }

    public void RigisterCharacter(BaseController character)
    {
        controllers.Add(character);
        Debug.Log($"[BattleManager] Rigister Charcter:{character.data.Name}");
        if (controllers.Count >= 2)
        {
            InitializeBattle();
        }
        var iconobj = Instantiate(timeLineIconPrefab, actionBarPanel);
        var icon = iconobj.GetComponent<TimeLineIcon>();
        icon.Bind(character);


        timeLineIcons.Add(character, icon);
        //character.data.ActionValue = character.data.MaxActionValue; //初始行动值设为最大值
        FindObjectOfType<TimeLineUI>()?.BuildCache(); 
    }

    void InitializeBattle()
    {
        foreach (var character in controllers)
        {
            character.data.ActionValue = character.data.MaxActionValue; //初始行动值设为最大值
        }
        isBattleReady = true;
        //开局就刷新一次 UI 排列
        UpdateTimeLineUI(controllers
            .Where(c => !c.isDead)
            .OrderBy(c => c.data.ActionValue)
            .ToList());

        Debug.Log("[BattleManager] Battle Ready!");
       
    }
    void updateActionValues()
    {
        controllers.RemoveAll(c => c == null); //移除已销毁的角色//清掉Destroy后留下的空位，防止空引用
        if (isActing) return;
        //减少行动值，当行动值到达0或这者低于0时，触发行动
        foreach (var c in controllers)
        {
            if (c.isDead) //死亡角色不行动
                continue;
            //速度越快，行动值减少越快
            c.data.ActionValue -= c.data.Speed / 0.75f;
            // c.data.ActionValue = Mathf.Max(0, c.data.ActionValue);
            // Debug.Log($"{c.Name}的ActionValue={c.ActionValue:F2}");
        }

        //找出行动值最小的角色(谁最接近0)
        var ordered = controllers
            .Where(c => !c.isDead)
            .OrderBy(c => c.data.ActionValue)
            .ToList();
       // Debug.Log("Ordered:" + string.Join(",", ordered.Select(x => x.data.Name)));
       
        //更新ui
        var nextActor= ordered.FirstOrDefault();
        if(nextActor != null && nextActor.data.ActionValue <= 0 && !isActing)
        {
            //先确定当前行动者（让ui有事实源）
            SetCurrentActor(nextActor);

            //用当前ordered 推一次nextActor 之后的顺序，通知ui刷新
            UpdateTimeLineUI(ordered);
            OnTimeLineOrdered?.Invoke(ordered);

            StartCoroutine(PerformTrun(nextActor));
        }
        else
        {
            //更新ui//普通 排序更新
            UpdateTimeLineUI(ordered);
            OnTimeLineOrdered?.Invoke(ordered);
        }
    }
   
    IEnumerator PerformTrun(BaseController actor)
    {
        //SetCurrentActor(actor);

        //战斗结束终止行动
        if (battleEnded)
            yield break;

        isActing = true;
        actor.data.isActing = true;
        Debug.Log($"{actor.data.Name}开始行动！");
        
        //模拟执行动作
        yield return new WaitForSeconds(1.5f); //等待1秒，模拟行动时间
        if (actor == null || actor.data == null || actor.data.isDead)
        {
            isActing = false;
            yield break;
        }
        //
        if (actor.isPlayer)
        {
            //等待玩家输入
            Debug.Log("等待玩家输入指令...");
            yield return StartCoroutine(WaitForPlayerAction(actor));
            if (actor == null || actor.data == null || actor.data.isDead)
            {
                isActing = false;
                yield break;
            }
        }
        else
        {
            //敌人自动行动
            yield return new WaitForSeconds(0.5f);
            var target = controllers.FirstOrDefault(c => c.data.Team != actor.data.Team && !c.isDead);
            if (target != null)
            {
                target.TakeDamage(actor.data.Attack);
                CheckBattleEnd(actor, target);
            }
            yield return new WaitForSeconds(1f);
        }
        //行动完成后恢复行动值      
        actor.data.ActionValue = actor.data.MaxActionValue;
        isActing = false;
        
        Debug.Log($"{actor.data.Name}结束行动！");

    }
    IEnumerator WaitForPlayerAction(BaseController actor)
    {
        bool actionChosen = false;
        Debug.Log("press the space key attack the enemy");
        while (!actionChosen)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var target = controllers.FirstOrDefault(c => c.data.Team != actor.data.Team && !c.isDead);
                if (target != null)
                {
                    target.TakeDamage(actor.data.Attack);
                    //Debug.LogFormat($"{actor.Name} attack the {target.Name} rise {actor.Attack} damage");
                    Debug.Log($"{actor.data.Name} attack the {target.data.Name} rise {actor.data.Attack} damage");
                    CheckBattleEnd(actor, target);
                }
                actionChosen = true;
            }

            yield return null;
        }
    }
    void CheckBattleEnd(BaseController attacker, BaseController target)
    {
        if (target.data.Hp <= 0)
        {
            battleEnded = true;
            Debug.Log($"Battle Finish!");
            if (attacker.isPlayer)
            {
                Debug.Log($"You Win!");
            }
            else
            {
                Debug.Log($"You Lose!");
            }
        }
    }
    void UpdateTimeLineUI(List<BaseController> ordered)
    {
        
        for(int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            if (!timeLineIcons.TryGetValue(c, out var icon)||icon==null) continue;

            //直接把prefab 变成actionbarpanel 的子物体

            icon.transform.SetParent(actionBarPanel, false);

            //改兄弟顺序。让layoutGroup 自动重排
            icon.transform.SetSiblingIndex(i); 

            
        }
        //强制刷新布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarPanel);
    }
    public void NotifyDeath(BaseController dead)
    {
        StartCoroutine(HandleDeathCoroutine(dead));
    }

    private IEnumerator HandleDeathCoroutine(BaseController dead)
    {
        //等今天过去再删除 用来debug DOTween 用的
        Debug.Log($"[Death]{dead.data.Name} removed from battle");

        if (dead == null) yield break;

        //先标记为死亡&禁用，避免参与其他逻辑
        dead.data.isDead = true;
        dead.enabled = false;
        //如果当前行动者是死亡角色，清除当前行动者
        if (CurrentActor == dead)
        {
            SetCurrentActor(null);
        }

        //从所有逻辑入口中移除（最关键）
        controllers.Remove(dead);

       

        //ui和列表移除，避免后续tick/行动中被访问到
        //ui层.移除时间轴图标
        if (timeLineIcons.TryGetValue(dead, out var icon) && icon != null)
        {
            Destroy(icon.gameObject);
        }
        timeLineIcons.Remove(dead);
      
        //刷新时间轴(避免ui 还显示旧顺序)
        UpdateTimeLineUI(controllers
            .Where(c => c != null && !c.data.isDead)
            .OrderBy(c => c.data.ActionValue)
            .ToList());

        //表现层，隐藏冰摧毁角色本体
        dead.gameObject.SetActive(false);

        //等待一帧真正销毁（防协程美剧中途爆炸）
        yield return null;

        Destroy(dead.gameObject);
        
        
      
    }
    //设置当前行动者，并触发事件 也是唯一改变_currentActor的地方
    private void SetCurrentActor(BaseController next)
    {
        if (_currentActor == next) return;
        var prev = _currentActor;
        _currentActor = next;

        Debug.Log($"[Battle] Invoke OnActionChanged: {(prev ? prev.name : "null")} -> {(next ? next.name : "null")}");
        

        /*var del = OnActionChanged;
        Debug.Log($"[Battle] OnActionChanged subs={(del == null ? 0 : del.GetInvocationList().Length) }");*/
        OnActionChanged?.Invoke(prev, _currentActor);
    }
}



