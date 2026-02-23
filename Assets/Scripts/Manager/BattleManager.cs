using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance{ get; private set; }
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

    private bool _reorderRequested;
    private readonly List<BaseController> _lastPublishedOrdered = new();
    //当前行动者
    private BaseController _currentActor;
    //当前目标
    private BaseController _currentTarget;

    [SerializeField] private VerticalLayoutGroup actionBarLayout;
    [SerializeField] private float timelineMoveTime= 0.25f;
    [SerializeField] private BattleFormation formation;
    //点击检测相关
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask enemyClickMask;//敌人点击层
    [SerializeField] private float clickMaxDistance = 200f;//点击检测最大距离

    

    private bool _timelineInitialized = false;
    private Tween _moveTween;//防止重入

    public BaseController CurrentActor=>_currentActor;

    void Awake()
    {
        Instance = this;
        if(mainCamera==null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        
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
    private void LateUpdate()
    {
        if (!_reorderRequested) return;
        _reorderRequested = false;

        //统一计算本帧最终顺序
        var ordered = controllers
            .Where(c => c != null && !c.isDead && c.data != null && !c.data.isDead)
            .OrderBy(c => c.data.ActionValue)
            .ToList();

        //顺序完全没变：不发布，不做重排/动画（解决每个回合卡一下）
        if (isSameOrder(ordered, _lastPublishedOrdered))
            return;

        _lastPublishedOrdered.Clear();
        _lastPublishedOrdered.AddRange(ordered);

        PublishOrdered(ordered);
    }

    public void RegisterCharacter(BaseController character)
    {
        controllers.Add(character);
        Debug.Log($"[BattleManager] Register Charcter:{character.data.Name}");


       /* if (!isBattleReady&&controllers.Count >= 2)
        {
            InitializeBattle();
        }*/
        var iconobj = Instantiate(timeLineIconPrefab, actionBarPanel);
        var icon = iconobj.GetComponent<TimeLineIcon>();
        icon.Bind(character);
        if (isBattleReady)
        {
            TryPlaceIntoFormation(character);
        }else if (controllers.Count >= 2)
        {
            InitializeBattle();
        }

            timeLineIcons.Add(character, icon);

        //character.data.ActionValue = character.data.MaxActionValue; //初始行动值设为最大值
        FindObjectOfType<TimeLineUI>()?.BuildCache();

        RequestReorder();//最后一帧搞一下
    }

    void InitializeBattle()
    {
        if (isBattleReady) return;
        foreach (var character in controllers)
        {
            character.data.ActionValue = character.data.MaxActionValue; //初始行动值设为最大值
        }

        PlaceAllExistingControllersIntoFormation();

        isBattleReady = true;
        //开局就刷新一次 UI 排列
        RequestReorder();

        Debug.Log("[BattleManager] Battle Ready!");
       
    }

    //每次算出ordered，都统一走一个函数
    private void PublishOrdered(List<BaseController> ordered)
    {
        UpdateTimeLineUI(ordered);
        OnTimeLineOrdered?.Invoke(ordered);
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
            //统一发布一次（位置和next都更新）
              RequestReorder();//只请求刷新一次

            StartCoroutine(PerformTrun(nextActor));
        }
        else
        {
            //更新ui//普通 排序更新//平时也统一发布
           RequestReorder();
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
            var target = GetRandomEnemyTarget(actor);
            if (target != null)
            {
                Debug.LogError($"[ATTACK] attacker={actor.data.Name} target={(target ? target.data.Name : "null")} targetType={target?.GetType().Name}");
                target.TakeDamage(actor.data.Attack);
                Debug.LogError("[ATTACK] after TakeDamage");
                CheckBattleEnd(actor, target);
            }
            yield return new WaitForSeconds(1f);
        }
        //行动完成后恢复行动值      
        actor.data.ActionValue = actor.data.MaxActionValue;
        RequestReorder();
        isActing = false;
        
        Debug.Log($"{actor.data.Name}结束行动！");

    }
    IEnumerator WaitForPlayerAction(BaseController actor)
    {
        bool actionChosen = false;
        Debug.Log("press the space key attack the enemy");
        //回合开始先自动选一个目标（如果当前目标无效的话）
        AutoPickTargetIfNeeded(actor);

        while (!actionChosen)
        {
            //鼠标点击选目标
            HandleMouseClickSelect(actor);
            //Tab键切换目标
            CycleEnemyTarget(actor);
            //如果目标死了/无效了，自动选一个目标
            AutoPickTargetIfNeeded(actor);

            //按空格攻击
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var target =_currentTarget;
                if (IsValidEnemyTarget(actor,target))
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
        if (target.data.Hp > 0) return;
        //不要立刻battle Ended=true
        //判断是否还有任何存在的单位
        bool enemyAlive = controllers.Any(c=>c!=null&&c.data!=null&&!c.data.isDead&&!c.isDead&&c.data.Team!=attacker.data.Team);
        bool allyAlive = controllers.Any(c=>c!=null&&c.data!=null&&!c.data.isDead&&!c.isDead&&c.data.Team==attacker.data.Team);

        if (!enemyAlive||!allyAlive)
        {
            battleEnded = true;
            Debug.Log($"Battle Finish!");
            Debug.Log(!enemyAlive ? "You Win!" : "You Lose!");
        }
    }
    void UpdateTimeLineUI(List<BaseController> ordered)
    {
        if (ordered == null || ordered.Count == 0) return;
        Debug.Log($"[UpdateTimeLineUI] updateTimeLineUI called");


        //0)第一个，只让layout 把位置摆正，防止初次乱飞
        if(!_timelineInitialized)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                var c = ordered[i];
                if (!timeLineIcons.TryGetValue(c, out var icon) || icon == null) continue;
                //icon.transform.SetParent(actionBarPanel, false);
                icon.transform.SetSiblingIndex(i);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarPanel);
            _timelineInitialized = true;
            return;
        }
        //如果上一次还在移动，先杀掉（避免重入）
        _moveTween?.Kill();
        _moveTween = null;

        //1）记录旧位置(排序前)
        var oldPositions = new Dictionary<RectTransform, Vector2>(timeLineIcons.Count);

        foreach (var kv in timeLineIcons)
        {
            if (kv.Value == null) continue;

            var rt=kv.Value.GetComponent<RectTransform>();
            if (rt != null)
                oldPositions[rt] = rt.anchoredPosition;
        }
        //2)重新排序（layout 重新计算位置）
        for (int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            if (!timeLineIcons.TryGetValue(c, out var icon)||icon==null) continue;

            //直接把prefab 变成actionbarpanel 的子物体
            icon.transform.SetParent(actionBarPanel, false);

            //改兄弟顺序。让layoutGroup 自动重排
            icon.transform.SetSiblingIndex(i);  
        }
        //3)强制刷新布局-此时每个icon的anchoredPosition 已经变成“目标”位置了
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarPanel);

        //4）缓存目标位置，（排序后，被拉回就位置以前）
        var targetPos = new Dictionary<RectTransform, Vector2>(oldPositions.Count);
        foreach(var kv in oldPositions)
        {
            var rt= kv.Key;
            if (rt == null) continue;
            targetPos[rt] = rt.anchoredPosition;
        }

        //5)Added! 关掉layoutGroup，避免tween动画时 layoutGroup干扰位置
        if(actionBarLayout !=null)actionBarLayout.enabled = false;

        //6)把icon位置拉回旧位置（排序后，先都拉回原来位置）
        foreach (var kv in oldPositions)
        { 
            if(kv.Key!=null)
                kv.Key.anchoredPosition = kv.Value;
        }

        //7)tween 到新位置(用一个sequence管理)
        var seq=DOTween.Sequence();
        foreach (var kv in targetPos)
        {
            var rt=kv.Key;
            var pos = kv.Value;
            if (rt == null) continue;

            //避免同一个rt重复挂多个移动tween
            rt.DOKill(false);//只杀掉rt的位移tween

            seq.Join(rt.DOAnchorPos(pos, timelineMoveTime).SetEase(Ease.OutCubic));
        }
        //8） 动画结束后，重新打开layoutGroup（如果有的话），让布局恢复正常
        seq.OnComplete(() =>
        {
            if (actionBarLayout != null) actionBarLayout.enabled = true;
           //重新让layout对其一次
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarPanel);
        });

        _moveTween = seq;
    }
    public void NotifyDeath(BaseController dead)
    {
        if(_currentTarget==dead)
            SetCurrentTarget(null);
        StartCoroutine(HandleDeathCoroutine(dead));
    }
    private IEnumerator HandleDeathCoroutine(BaseController dead)
    {
        if(_currentTarget==dead)SetCurrentTarget(null);

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
        //从站位中释放
        formation.Release(dead, out _);

        //ui和列表移除，避免后续tick/行动中被访问到
        //ui层.移除时间轴图标
        if (timeLineIcons.TryGetValue(dead, out var icon) && icon != null)
        {
            Destroy(icon.gameObject);
        }
        timeLineIcons.Remove(dead);

        //刷新时间轴(避免ui 还显示旧顺序)
        RequestReorder();

        //表现层，隐藏并摧毁角色本体
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
        Debug.Log($"[Battle] OnActionChanged subs={(OnActionChanged == null ? 0 : OnActionChanged.GetInvocationList().Length)} prev={prev?.data?.Name} cur={_currentActor?.data?.Name}");
        OnActionChanged?.Invoke(prev, _currentActor);
        
    }
  private void RequestReorder()
    {
        _reorderRequested = true;
    }
   
   public void RegisterController(BaseController ctrl)
    {
        if (ctrl == null) return;
        if (controllers.Contains(ctrl)) return;//防止重复注册


        RegisterCharacter(ctrl);
    }
    private static bool isSameOrder(List<BaseController> a, List<BaseController> b)
    {
        if (a == null || b == null) return false;
        if(a.Count!=b.Count) return false;
        for(int i = 0; i < a.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]))
                return false;
        }
        return true;
    }
  /*  public void UnregisterContoller(BaseController ctrl)
    {
        bool wasAcive = (ctrl == _currentActor);
        controllers.Remove(ctrl);

        if (wasAcive) {
            //选一个新的active（通常是当前排序当中最靠前的）
            //触发On Action Changed（prev,cur）
           SetCurrentActor(null);
        }

        RequestReorder();
    }*/
  //最小占槽并且对齐
  public bool TryPlaceIntoFormation(BaseController ctrl)
    {
        if (ctrl == null || ctrl.data == null || formation == null) return false;

        var team = ctrl.data.Team;//根据队伍找对应槽位
        int slotIndex = formation.FindFirstEmpty(team);
        if (slotIndex < 0) return false;

        if (!formation.TryOccupy(team, slotIndex, ctrl)) return false;

        var anchor = formation.GetAnchor(team, slotIndex);
        ctrl.transform.position = anchor.position; //直接放到目标位置，后续可以加个动画
        return true;
    }
    public void PlaceAllExistingControllersIntoFormation()
    {
        if(formation==null) return;

        foreach(var c in controllers)
        {
            if (c == null || c.data == null) continue;
            if (c.data.isDead || c.isDead) continue;

            //直接尝试占槽（不成功就算了，先占了再说，后续可以加个提示或者自动分配）又或者是contains判断
            TryPlaceIntoFormation(c);
        }
    }
    //Tab键切换目标
    private void CycleEnemyTarget(BaseController attcker)
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        var list = formation.GetAliveEnemiesInPreferredOrder();
        if (list.Count == 0)
        {
            SetCurrentTarget(null);
            return;
        }
        int idx = list.IndexOf(_currentTarget);
        idx = (idx + 1) % list.Count;
        SetCurrentTarget(list[idx]);
    }
    //设置当前目标，并触发事件 也是唯一改变_currentTarget的地方
    private void SetCurrentTarget(BaseController target)
    {
        if (_currentTarget == target) return;

        if(_currentTarget!=null)_currentTarget.SetTargeted(false);
        _currentTarget = target;
        if (_currentTarget != null) _currentTarget.SetTargeted(true);

        Debug.Log($"[Target]->{(_currentTarget ? _currentTarget.data.Name : "null")}");
    }
    private bool IsValidEnemyTarget(BaseController actor,BaseController target)
    {
        if (actor == null || actor.data == null) return false;
        if (target == null || target.data == null) return false;
        if (target.data.isDead || target.isDead) return false;
        return target.data.Team != actor.data.Team;
    }
    private void AutoPickTargetIfNeeded(BaseController actor)
    {
        if(IsValidEnemyTarget(actor,_currentTarget))return;//当前目标有效，不需要自动选
        //从formation获取按照槽位优先级排序的存活敌人
        var list = formation.GetAliveEnemiesInPreferredOrder();
        SetCurrentTarget(list.Count > 0 ? list[0]:null);
    }
    //点击检测，选敌人
    private void HandleMouseClickSelect(BaseController actor)
    {
        if (mainCamera == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray=mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray,out RaycastHit hit,clickMaxDistance,enemyClickMask))
        {
            
            var targetable = hit.collider.GetComponentInParent<Targetable>();
            if (targetable != null && IsValidEnemyTarget(actor, targetable.controller))
            {
                SetCurrentTarget(targetable.controller);
            }
        }

    }
    private BaseController GetRandomEnemyTarget(BaseController attacker)
    {
        if (attacker == null) return null;

        var candidates = controllers
            .Where(c => c != null && c.data != null && !c.data.isDead && !c.isDead && c.data.Team != attacker.data.Team)
            .ToList();
        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }
        

}



