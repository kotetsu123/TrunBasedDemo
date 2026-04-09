using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;


public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance{ get; private set; }
    //c#event 广播，当行动者改变时触发
    public event Action<BaseController, BaseController> OnActionChanged;
    public event Action<List<BaseController>> OnTimeLineOrdered;
    //目标锁定TargetCircle 用event
    public event Action<BaseController> OnTargetChanged;
    public event Action<bool> OnInputStateChanged;
    //当前actor 高光
    public event Action<BaseController>OnCurrentActorChanged;
    //向外广播战斗是否结束
    public event Action<BattleResultPayload> OnBattleEnded;
    //道具数量改变
    public Action<ItemData, int> OnItemCountChanged;

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
    private  BaseController  _currentTarget;
    //当前目标类型
    private SkillTargetType _currentTargetType;

    
    private bool actionChosen = false;

    [SerializeField] private VerticalLayoutGroup actionBarLayout;
    [SerializeField] private float timelineMoveTime= 0.25f;
    [SerializeField] private BattleFormation formation;
    //点击检测相关   
    [SerializeField] private BattleTargetSelector targetSelector;
    //生成相关
    [SerializeField] private BattleSpawner spawner;
    //选中模块相关
    [SerializeField] private TargetCircle targetCirclePrefab;
    [SerializeField] private Canvas worldSpaceCanvs;
    //战斗指令模块相关
    [SerializeField] private BattleCommandPanel commandPanel;
    [SerializeField] private SkillPanelController skillPanel;
    //弹出技能名字相关
    [SerializeField] private SkillNamePopController skillNamePopUp;
    //道具相关
    [SerializeField] private ItemPanelController itemPanel;
    [SerializeField] private List<ItemData> startingItems=new List<ItemData>();
    [SerializeField] private List<int> startItemCounts = new List<int>();


    //经验相关
    [SerializeField] private LevelUpPopController levelUpPopup;
    private List<LevelUpResult> _lastLevelUpResults= new List<LevelUpResult>();

    public IReadOnlyList<LevelUpResult>LastLevelUpResults=> _lastLevelUpResults;


    private Dictionary<ItemData, int> _itemCounts = new Dictionary<ItemData, int>();

    private CommandType _currentCommand=CommandType.None;

    private TargetCircle _targetCircle;
    

    private bool _timelineInitialized = false;
    private Tween _moveTween;//防止重入

    private SkillData _selectedSkill;
    private ItemData _selectedItem;

    public BaseController CurrentActor=>_currentActor;
    public BaseController CurrentTarget => _currentTarget;
    public CommandType CurrentCommand=> _currentCommand;
   
    void Awake()
    {
        Instance = this;
        

        if (targetCirclePrefab != null&&worldSpaceCanvs!=null)
        {
            _targetCircle=Instantiate(targetCirclePrefab,worldSpaceCanvs.transform);
            _targetCircle.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (commandPanel != null)
            commandPanel.OnCommandSelected += HandleCommandSelected;
        skillPanel.OnSkillSelected += HandleSkillSelected;
        skillPanel.OnCancel += HandleSkillCancel;
        itemPanel.OnItemSelected += HandleItemSelected;
    }
    private void OnDisable()
    {
        if (commandPanel != null)
            commandPanel.OnCommandSelected -= HandleCommandSelected;
        skillPanel.OnSkillSelected -= HandleSkillSelected;
        skillPanel.OnCancel -= HandleSkillCancel;
        itemPanel.OnItemSelected -= HandleItemSelected;
    }
   
    private void Start()
    {
        
    }

    void Update()
    {
        if (battleEnded) return;//如果战斗结束，跳过本次更新
        if (isActing) return;//正在行动中，跳过本次更新
        if (!isBattleReady) return;//战斗未准备好，跳过本次更新

        tick++;
        //每隔10个时间刻度，所有角色增加行动值
        if (tick % 10 == 0)
        {
            updateActionValues();
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

        //没上场的不注册
        if (character == null || character.data == null) return;
        //重复调用guard 去重guard
        if (controllers.Contains(character))
        {          
            return;
        }
        if (timeLineIcons.ContainsKey(character))
        {
            return;
        }
        if (!character.data.isOnField) return;
        Debug.Log($"[RegisterCharacter] {character?.data?.Name} go={character?.name}");
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

        character.OnRevied += HandleCharacterRevived;
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
        InitItemInventory();
        _lastLevelUpResults.Clear();

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
            if (c == null || c.data == null) continue;
            if (c.isDead) continue; //死亡角色不行动
            if (!c.data.isOnField) continue;
            //速度越快，行动值减少越快
            c.data.ActionValue -= c.data.Speed / 0.75f;           
        }

        //找出行动值最小的角色(谁最接近0)
        var ordered = controllers
            .Where(c => !c.isDead&&c.data!=null&&c!=null&&c.data.isOnField)
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
            if (actor != null && actor.data != null)
                actor.data.isActing = false;
            yield break;
        }
        //
        if (actor.data.Team==Team.Player)
        {
            //等待玩家输入
            Debug.Log("等待玩家输入指令...");
            yield return StartCoroutine(WaitForPlayerAction(actor));
            if (actor == null || actor.data == null || actor.data.isDead)
            {
                isActing = false;
                if(actor != null && actor.data != null)
                    actor.data.isActing = false;
                yield break;
            }
        }
        else
        {
            /*//敌人自动行动
            yield return new WaitForSeconds(0.5f);
            var target = GetRandomEnemyTarget(actor);
            if (target != null)
            {
                //Debug.LogError($"[ATTACK] attacker={actor.data.Name} target={(target ? target.data.Name : "null")} targetType={target?.GetType().Name}");
                target.TakeDamage(actor.data.Attack);
                //Debug.LogError("[ATTACK] after TakeDamage");
                CheckBattleEnd(actor, target);
            }
            yield return new WaitForSeconds(1f);*/
            yield return StartCoroutine(WaitForEnemyAction(actor));
            if (actor == null || actor.data == null || actor.data.isDead)
            {
                isActing = false;
                if (actor != null && actor.data != null)
                    actor.data.isActing = false;
            }
        }
        //行动完成后恢复行动值      
        if (!battleEnded)
        {
            actor.data.ActionValue = actor.data.MaxActionValue;
            RequestReorder();
        }
        actor.data.isActing = false;
        isActing = false;
        
        Debug.Log($"{actor.data.Name}结束行动！");
    }
    IEnumerator WaitForPlayerAction(BaseController actor)
    {
        actionChosen = false;

        Debug.Log("press the space key attack the enemy");
        _currentCommand = CommandType.None;
        //回合开始先自动选一个目标（如果当前目标无效的话）
       
       targetSelector.AutoPickTargetIfNeeded(actor);
        if (commandPanel != null)
        {
            commandPanel.Show();
        }
        while (!actionChosen)
        {

            //debug 经验系统用
            if (Input.GetKeyDown(KeyCode.T))
            {
                actor.data.GainExp(120);
            }

            if (battleEnded) {
                actionChosen=true;
                yield break;
            }
                
            //skillPanel 返回commandPanel
            HandleSkillCancel();
            //itemPanel 返回commandPanel
            HandleItemCancel();
            //选中返回
            HandleCancelInput();

            //还没选指令时，只等菜单按钮
            if (_currentCommand == CommandType.None)
            {
                yield return null;
                continue;
            }
            //选了attack 之后，才允许目标切换和确认
            if (_currentCommand == CommandType.Attack)
            {
                targetSelector.HandleTargetSelectionInput(actor,_currentTargetType);
                // 按空格攻击
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    var target = _currentTarget;
                    if (targetSelector.IsValidEnemyTarget(actor, target))
                    {
                        ShowSkillName("Attack");

                        target.TakeDamage(actor.data.Attack);
                        //Debug.LogFormat($"{actor.Name} attack the {target.Name} rise {actor.Attack} damage");
                        Debug.Log($"{actor.data.Name} attack the {target.data.Name} rise {actor.data.Attack} damage");
                        CheckBattleEnd(actor, target);
                        actionChosen = true;
                    }                   
                }
            }
            //选了skill 之后先用q来进行测试
            else if (_currentCommand == CommandType.Skill) {

                if (_selectedSkill == null){
                    yield return null;
                    continue;
                }
                targetSelector.HandleTargetSelectionInput(actor, _currentTargetType);
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    var target = _currentTarget;
                    if (_currentTargetType == SkillTargetType.Self)
                    {
                        target = actor;
                    }
                    if (targetSelector.IsValidTarget(actor, target, _currentTargetType))
                    {
                        actor.UseSkill(_selectedSkill, target);
                        CheckBattleEnd(actor, target);
                        actionChosen = true;
                    }
                }
               
                /*if (_currentTargetType == SkillTargetType.Self)
                {
                    
                   // targetSelector.HandleTargetSelectionInput(actor, _currentTargetType);
                    if (Input.GetKeyDown(KeyCode.Space))
                    {                     
                        actor.UseSkill(_selectedSkill, actor);
                        CheckBattleEnd(actor, actor);
                        actionChosen = true;
                    }
                }
                else// 需要选敌人的技能     
                {
                    targetSelector.HandleTargetSelectionInput(actor,_currentTargetType);
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        var target = _currentTarget;                       
                        if (targetSelector.IsValidTarget(actor, target,_currentTargetType))
                        {
                            
                            actor.UseSkill(_selectedSkill, target);
                            CheckBattleEnd(actor, target); 
                            actionChosen = true;
                        }
                    }
                }*/
                
            }
            else if (_currentCommand == CommandType.Item)
            {
                if (_selectedItem == null)
                {
                    yield return null;
                    continue;
                }
                targetSelector.HandleTargetSelectionInput(actor, _currentTargetType);

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    var target = _currentTarget;

                    if (targetSelector.IsValidAllyTarget(actor, target))
                    {
                        UseItem(actor, _selectedItem, target);

                        CheckBattleEnd(actor, target);
                        actionChosen = true;
                    }
                }
            }
            else if (_currentCommand == CommandType.Run)
            {
                HandleRun(actor);
                actionChosen = true;
            }
                yield return null;
        }
        if (commandPanel != null)
            commandPanel.Hide();
    }
    IEnumerator WaitForEnemyAction(BaseController actor)
    {
        yield return new WaitForSeconds(0.5f);//停顿一下

        var skill=ChooseEnemySkill(actor);
        /*if (skill == null)
        {
            Debug.LogWarning($"[EnemyAI] {actor?.data?.Name} has no usable skill.");
            yield break;
        }*/
        var target = ChooseEnemyTarget(actor, skill);
       /* if (target == null)
        {
            Debug.LogWarning($"[EnemyAI] {actor?.data?.Name} could not find a valid target for {skill.skillName}.");
            yield break;
        }*/

        actor.UseSkill(skill, target);
        CheckBattleEnd(actor, target);

        yield return new WaitForSeconds(1f);
    }
    private SkillData ChooseEnemySkill(BaseController actor)
    {
        var skills = actor.Skills;
      
        if (actor.data.Hp < actor.data.MaxHp * 0.5f&&actor.healUsedCount<2)
        {
            var heal = FindSkillByType(actor.Skills, SkillType.Heal);
       
            if (heal!=null&&GetBestHealTarget(actor)!=null)
            {
                return heal;
            }
        }
        var damageSkills = FindSkillsByType(actor.Skills, SkillType.Damage);
       
        if (damageSkills.Count == 0)
            return null;
        int index = UnityEngine.Random.Range(0, damageSkills.Count);      
        return damageSkills[index];
    }
    //单个
    private SkillData FindSkillByType(IReadOnlyList<SkillData> skills,SkillType type)
    {
        if (skills == null) return null;
         
        for(int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill != null && skill.skillType == type)
                return skill;
        }
        return null;
    }
    private List<SkillData> FindSkillsByType(IReadOnlyList<SkillData> skills,SkillType type)
    {
        List<SkillData> result = new List<SkillData>();

        if (skills == null) return result;

        for(int i = 0; i < skills.Count; i++)
        {
            var skill = skills[i];
            if (skill != null && skill.skillType == type)
            {
                result.Add(skill);
            }
        }
        return result;
    }
    private BaseController ChooseEnemyTarget(BaseController actor,SkillData skill)
    {
        if (actor == null || skill == null)
            return null;
        switch (skill.targetType)
        {
            case SkillTargetType.EnemySingle:
                //敌人的敌人=玩家 这里使用的函数其实是通用函数
                return GetRandomEnemyTarget(actor);
            case SkillTargetType.AllySingle:
                if (skill.skillType == SkillType.Heal)
                    return GetBestHealTarget(actor);

                return GetRandomAllyTarget(actor);
            case SkillTargetType.Self:
                return actor;
            default:
                return actor;
        }
    }
    private BaseController GetBestHealTarget(BaseController actor)
    {
        if (actor == null || actor.data == null)
            return null;

        BaseController best = null;
        float lowestHpRatio=float.MaxValue;//先设置一个极大的数，当作当前最低血量比例

        foreach(var unit in controllers)
        {
            if (unit == null || unit.data == null) continue;
            if (unit.data.Team != actor.data.Team) continue;
            if (unit.isDead || unit.data.isDead) continue;
            if (!unit.data.isOnField) continue;
            if (unit.data.Hp >= unit.data.MaxHp) continue;

            //当前这个单位的血量百分比
            float hpRatio=(float)unit.data.Hp/unit.data.MaxHp;

            if (hpRatio < lowestHpRatio)
            {
                //对比
                lowestHpRatio = hpRatio;
                best = unit;
            }
        }
        return best;
    }
    /*private List<BaseController> GetAliveAllies(BaseController actor)
    {
        List<BaseController> result = new List<BaseController>();
        if (actor == null || actor.data == null)
            return result;
        foreach (var unit in )
    }*/
    //检测战斗是否结束（任何一方全灭）
    void CheckBattleEnd(BaseController attacker, BaseController target)
    {
        if (battleEnded) return;
        if (target == null || target.data == null) return;
        if (target.data.Hp > 0) return;
        _lastLevelUpResults.Clear();
        //不要立刻battle Ended=true
        //判断是否还有任何存在的单位
        bool enemyAlive = controllers.Any(c=>c!=null&&c.data!=null&&!c.data.isDead&&!c.isDead&&c.data.Team==Team.Enemy);
        bool allyAlive = controllers.Any(c=>c!=null&&c.data!=null&&!c.data.isDead&&!c.isDead&&c.data.Team==Team.Player);

        if (!enemyAlive||!allyAlive)
        {
            var result = !enemyAlive ? BattleResult.Win : BattleResult.Lose;    
           HandleBattleEnd(result);          
        }
    }
    //战斗结束后的处理
    private void HandleBattleEnd(BattleResult result)
    {
        if (battleEnded) return;

         battleEnded = true;
        //强制关闭输入
        OnInputStateChanged?.Invoke(false);
        //清空上一次升级结果
        _lastLevelUpResults.Clear();
        //胜利时发经验
        if (result == BattleResult.Win)
        {
            const int rewardExp = 120;
            _lastLevelUpResults.AddRange(AwardPartyExp(rewardExp));
        }
        //结算快照
        var snapshots = BuildPartySnapShots();
        var payload = new BattleResultPayload(result, snapshots);
        Debug.Log($"[BattleManager] snapshots count={snapshots?.Count ?? -1}");
        if (snapshots != null && snapshots.Count > 0)
            Debug.Log($"[BattleManager] first={snapshots[0].Name} hp={snapshots[0].hp}/{snapshots[0].maxhp}");
        //广播战斗结束
        OnBattleEnded?.Invoke(payload);
    }
    //经验值添加方法
    private List<LevelUpResult> AwardPartyExp(int amount)
    {
        var result=new List<LevelUpResult>();

        foreach(var c in controllers)
        {
            if (c == null || c.data == null)
                continue;
            if (c.data.Team != Team.Player)
                continue;
            int beforeLevel = c.data.Level;

            c.data.GainExp(amount);

            int afterLevel = c.data.Level;

            result.Add(new LevelUpResult(c.name, beforeLevel, afterLevel));
        }

        return result;
    }
   
    private void UseItem(BaseController actor,ItemData item,BaseController target)
    {
        if(actor==null||item==null||target==null) return;

        if (!CanUseItem(item))
        {
            Debug.Log($"[Item]{item.itemName}is out of stock");
            return;
        }
        ShowSkillName(item.itemName);

        switch (item.itemtype)
        {
            case ItemType.Heal:
                target.Heal(item.power);
                Debug.Log($"[ItemHeal]{_currentActor.name} used item {_currentTarget.name} has healed {item.power}HP {_currentTarget.name} current HP={_currentTarget.data.Hp} ");
                break;
        }
       ConsumeItem(item);
    }
    private List<CharacterResultSnapshot> BuildPartySnapShots()
    {
        var list= new List<CharacterResultSnapshot>();

        /*Debug.Log($"[Snapshots] controllers count={controllers.Count}");
        foreach (var c in controllers)
        {
            if (c == null) { Debug.Log("[Snapshots] null controller"); continue; }
            if (c.data == null) { Debug.Log($"[Snapshots] {c.name} data=NULL"); continue; }
            Debug.Log($"[Snapshots] {c.name} team={c.data.Team} name={c.data.Name}");
        }*/
        foreach (var c in controllers)
        {
            if (c == null || c.data == null) continue;
            if (c.data.Team!=Team.Player) continue;

            list.Add(new CharacterResultSnapshot
            {
                portrait=c.portrait,//只有这个是走basecontroller 拿数据。下面的都是从character拿的数据
                Name = c.data.Name,
                hp = c.data.Hp,
                maxhp = c.data.MaxHp,
                mp= c.data.Mp,
                maxmp= c.data.MaxMp,
                level= c.data.Level,
                exp= c.data.Exp,
                expToNextLevel= c.data.GetExpToNextLevel()
            });          
        }

        return list;
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
        if (dead == null) yield break;

        if (_currentTarget == dead)
            SetCurrentTarget(null);

        //先标记为死亡&禁用，避免参与其他逻辑
        dead.data.isDead = true;
        dead.data.Hp = 0;
        dead.enabled = false;

        //如果当前行动者是死亡角色，清除当前行动者
        if (CurrentActor == dead)
            SetCurrentActor(null);
  
        //从所有逻辑入口中移除（最关键）//时间轴
        controllers.Remove(dead);

        //ui和列表移除，避免后续tick/行动中被访问到
        //ui层.移除时间轴图标
        if (timeLineIcons.TryGetValue(dead, out var icon) && icon != null)
            Destroy(icon.gameObject);

        timeLineIcons.Remove(dead);
        //刷新时间轴(避免ui 还显示旧顺序)
        RequestReorder();

        //表现层，隐藏并摧毁角色本体//敌人的情况下
        if (dead.data.Team == Team.Enemy)
        {
            //从站位中释放//只有enmey才会
            formation.Release(dead, out _);
            dead.data.isOnField = false;

            dead.gameObject.SetActive(false);
            //等待一帧真正销毁（防协程美剧中途爆炸）
            yield return null;
            Destroy(dead.gameObject);
            //释放formation slot
            //formation.Release(dead.data.Team,deadSlotIndex);
            spawner?.TryFillOneEnemy();
            yield break;
        }
        else
        {
            //Player 不销毁
            //TODO:进入倒地状态，（濒死）状态机
            Debug.Log($"[Player Down Message] {dead.data.Name} is Down!");
            yield break;

        }
    }
    //设置当前行动者，并触发事件 也是唯一改变_currentActor的地方
    private void SetCurrentActor(BaseController next)
    {
        if (_currentActor == next) return;
        var prev = _currentActor;
        _currentActor = next;
        _currentCommand = CommandType.None;
        NotifyInputState();

        Debug.Log($"[Battle] Invoke OnActionChanged: {(prev ? prev.name : "null")} -> {(next ? next.name : "null")}");
        
        /*var del = OnActionChanged;
        Debug.Log($"[Battle] OnActionChanged subs={(del == null ? 0 : del.GetInvocationList().Length) }");*/
        Debug.Log($"[Battle] OnActionChanged subs={(OnActionChanged == null ? 0 : OnActionChanged.GetInvocationList().Length)} prev={prev?.data?.Name} cur={_currentActor?.data?.Name}");
        OnActionChanged?.Invoke(prev, _currentActor);

        OnCurrentActorChanged?.Invoke(_currentActor);

        bool playerTurn = (_currentActor != null && _currentActor.data != null && _currentActor.data.Team == Team.Player&&_currentCommand==CommandType.Attack||_currentCommand==CommandType.Skill);
        OnInputStateChanged?.Invoke(playerTurn);
       // UpdataTargetIndicatorVisibility();
    }
    //设置当前目标，并触发事件 也是唯一改变_currentTarget的地方
    public void SetCurrentTarget(BaseController target)
    {
        if (_currentTarget != null && _currentTarget != target)
            _currentTarget.SetTargeted(false);

        _currentTarget = target;

        if(_currentTarget !=null)
            _currentTarget.SetTargeted(true);

      /*  if (_currentTarget == target) return;
        //TODO: 这里重新选当前的敌人会取消选中。然后如果进行攻击的话。伤害不会判定。要么进行防呆处理：必须选中敌人才能攻击要么进行别的处理
        if (_currentTarget != null) _currentTarget.SetTargeted(false);
        _currentTarget = target;
        if (_currentTarget != null) _currentTarget.SetTargeted(true);
        Debug.Log($"[SetCurrentTarget] now = {_currentTarget?.data.Name}");*/
        OnTargetChanged?.Invoke(_currentTarget);

        //判断圈圈是否在player回合 并且发送广播
       // bool playerTurn = (_currentActor != null && _currentActor.data != null && _currentActor.data.Team == Team.Player && _currentCommand == CommandType.Attack || _currentCommand == CommandType.Skill);
        //OnInputStateChanged?.Invoke(playerTurn);

    }
    private bool CanTargetSelect()
    {
        if (_currentActor == null || _currentActor.data == null)
            return false;
        if (_currentActor.data.Team != Team.Player)
            return false;
        if (_currentCommand == CommandType.Attack)
            return true;
        if (_currentCommand == CommandType.Skill)
        {
            if (_selectedSkill == null) return false;
            // return _selectedSkill.targetType != SkillTargetType.Self;
            return true;
        }
        if (_currentCommand == CommandType.Item)
        {
            if (_selectedItem == null) return false;
            return true;
        }
        return false;
    }
    private void NotifyInputState()
    {
        OnInputStateChanged?.Invoke(CanTargetSelect());
    }
    private void RequestReorder()
    {
        _reorderRequested = true;
    }
   
   public void RegisterController(BaseController ctrl)
    {
        if (ctrl == null) return;

        if (controllers.Contains(ctrl)) return;//防止重复注册
        //if (ctrl == null || ctrl.data == null) return;
        //已上场=已占位 不要调整formation
        if (!ctrl.data.isOnField)
        {
            TryPlaceIntoFormation(ctrl);//仅用于“场景手动摆放但未占位的”旧流程
            if (!ctrl.data.isOnField) return;//不上场就不注册
        }
        
        Debug.Log($"[Register]{ctrl.data.Name} OnFiled={ctrl.data.isOnField} stack={Environment.StackTrace}");

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
    public void UnregisterContoller(BaseController ctrl)
    {
       if(ctrl == null) return;

       controllers.Remove(ctrl);

        if (timeLineIcons.TryGetValue(ctrl, out var icon) && icon != null)
            Destroy(icon.gameObject);

        timeLineIcons.Remove(ctrl);
        
        RequestReorder();
    }
    //最小占槽并且对齐
    public bool TryPlaceIntoFormation(BaseController ctrl)
    {
        if (ctrl == null || ctrl.data == null || formation == null) return false;

        if (ctrl.data.isOnField) return true;//已上场就不重复调整formation

        var team = ctrl.data.Team;//根据队伍找对应槽位
        int slotIndex = formation.FindFirstEmpty(team);
        if (slotIndex < 0) return false;

        if (!formation.TryOccupy(team, slotIndex, ctrl)) return false;

        ctrl.data.isOnField = true;

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

            //if (c.data.isOnField) continue;
            //直接尝试占槽（不成功就算了，先占了再说，后续可以加个提示或者自动分配）又或者是contains判断
            TryPlaceIntoFormation(c);
        }
    } 
    private BaseController GetRandomEnemyTarget(BaseController attacker)
    {
        if (attacker == null) return null;

        var candidates = controllers
            .Where(c => c != null && c.data != null && !c.isDead && c.data.Team != attacker.data.Team)
            .ToList();
        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }
    private BaseController GetRandomAllyTarget(BaseController actor)
    {
        var list=controllers
            .Where(c=>c!=null&&c.data!=null&&!c.isDead&&c.data.Team==actor.data.Team)
            .ToList();

        if (list.Count == 0)
            return null;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
  
   //战斗指令菜单相关
    private void HandleCommandSelected(CommandType cmd)
    {
        _currentCommand = cmd;
       
        switch (cmd)
        {
            case CommandType.Attack:
                Debug.Log("[Command] Attack selected");
                _currentTargetType = SkillTargetType.EnemySingle;
                targetSelector.AutoPickTargetIfNeeded(_currentActor);
                NotifyInputState();
                commandPanel.Hide();
                break;
            case CommandType.Skill:
                Debug.Log("[Command] Skill selected");
                commandPanel.Hide();
                skillPanel.Show(_currentActor);
                Debug.Log($"[SkillCommand]{_currentActor.name}");
                //TODO：改成打开skill面板
                return;//等待玩家选择技能

            case CommandType.Item:
                Debug.Log("[Command] Item selected");
                commandPanel.Hide();
                //TODO 
                itemPanel.Show();
                break;

            case CommandType.Run:
                Debug.Log("[Command] Run selected");
                commandPanel.Hide();
                //TODO
                break;
        }
    }
    private void HandleSkillSelected(SkillData skill)
    {
        _selectedSkill = skill;
        skillPanel.Hide();

        _currentCommand = CommandType.Skill;
        _currentTargetType = skill.targetType;

     
        if (skill.targetType == SkillTargetType.EnemySingle)
        {
            targetSelector.AutoPickTargetIfNeeded( _currentTarget);
            OnTargetChanged?.Invoke(_currentTarget);
            NotifyInputState();
            return;
        }        
        if (skill.targetType == SkillTargetType.AllySingle)
        {
            targetSelector.AutoPickAllyTargetIfNeeded(_currentActor);
            OnTargetChanged?.Invoke(_currentTarget);
            NotifyInputState();           
            return;
        }
        if (skill.targetType == SkillTargetType.AllyDeadSingle)
        {
            targetSelector.AutoPickDeadAllyTargetIfNeeded(_currentActor);
            OnTargetChanged?.Invoke(_currentTarget);
            NotifyInputState();
            return;
        }
        if (skill.targetType == SkillTargetType.Self)
        {
            targetSelector.AutoPickAllyTargetIfNeeded( _currentActor);
            OnTargetChanged?.Invoke(_currentTarget);
            NotifyInputState();
            return;
        }
        _currentCommand = CommandType.Skill;
        NotifyInputState();
    }
    private void HandleSkillCancel()
    {
        if(skillPanel==null||commandPanel==null) return;

        //只在skillpanel 打开的时候处理
        if (!skillPanel.IsOpen) return;
        //右键或者esc键
        if (!Input.GetMouseButtonDown(1) && !Input.GetKeyDown(KeyCode.Escape))
            return;

        CancelSkillSelection();
    }
    private void HandleItemCancel()
    {
        if (itemPanel == null || commandPanel == null) return;
        //只在itempanel 打开的时候处理
        if (!itemPanel.IsOpen) return;
        //右键或esc键
        if (!Input.GetMouseButtonDown(1) && !Input.GetKeyDown(KeyCode.Escape))
            return;
        CancelItemSelection();
    }
    public void HandleItemSelected(ItemData item)
    {
        if (item == null) return;
        if (!CanUseItem(item)) return;

        _selectedItem= item;
        _currentCommand = CommandType.Item;
        switch (item.itemtype)
        {
            case ItemType.Heal:
                _currentTargetType = SkillTargetType.AllySingle;
                targetSelector.AutoPickAllyTargetIfNeeded(_currentActor);
               
                break;
        }
        NotifyInputState();
    }
    private void HandleCancelInput()
    {
        if (!Input.GetMouseButtonDown(1) && !Input.GetKeyDown(KeyCode.Escape))
            return;
        //skill 目标选择中->回到skillpanel
        if (_currentCommand == CommandType.Skill && _selectedSkill != null)
        {
            _selectedSkill = null;
            _currentCommand = CommandType.None;
            NotifyInputState();

            skillPanel.Show(_currentActor);
            return;
        }
        //attack  目标选择中->返回commanPanel
        if (_currentCommand == CommandType.Attack)
        {
            _currentCommand = CommandType.None;
            NotifyInputState();

            commandPanel.Show();
            return;
        }
        if (_currentCommand == CommandType.Item)
        {
            _currentCommand = CommandType.None;
            NotifyInputState();

            itemPanel.Show();
            return;
        }
    }
    private void HandleCharacterRevived(BaseController ctrl) 
    {
        if (ctrl == null || ctrl.data == null) return;
        if (ctrl.data.isDead || ctrl.data.Hp <= 0) return;


       if(!controllers.Contains(ctrl))
        //重新加入时间轴
        RegisterController(ctrl);

        RequestReorder();

    }
    public void ShowSkillName(string skillName)
    {
        if (skillNamePopUp != null)
            skillNamePopUp.Play(skillName);
    }
    public void CancelSkillSelection()
    {
        skillPanel.Hide();
        commandPanel.Show();

        _currentCommand = CommandType.None;
        NotifyInputState();
    }
    public void CancelItemSelection()
    {
        itemPanel.Hide();
        commandPanel.Show();

        _currentCommand = CommandType.None;
        NotifyInputState();
    }
    private void HandleRun(BaseController actor)
    {
        battleEnded = true;

        _currentCommand = CommandType.None;
        _selectedSkill = null;
        _selectedItem = null;
        SetCurrentTarget(null);

        if (commandPanel != null)
            commandPanel.Hide();
        if (skillPanel != null)
            skillPanel.Hide();
        if(itemPanel!=null)
            itemPanel.Hide();

        NotifyInputState();

        ShowSkillName("Run");

        var snapshots = BuildPartySnapShots();
        var payload = new BattleResultPayload(BattleResult.Escape, snapshots);

        OnBattleEnded?.Invoke(payload);
    }
    //道具使用相关
    public List<ItemData> GetAvailableItems()
    {
        List<ItemData> result=new List<ItemData>();

        for(int i = 0; i < startingItems.Count; i++)
        {
            var item=startingItems[i];
            if (item == null) continue;
            if (result.Contains(item)) continue;

            result.Add(item);
        }
        return result;
    }
    private void InitItemInventory()
    {
        _itemCounts.Clear();

        int count = Mathf.Min(startingItems.Count, startItemCounts.Count);
        for(int i=0;i<count; i++)
        {
            var item= startingItems[i];
            int itemCount = startItemCounts[i];

            if (item == null) continue;

            _itemCounts[item] = Mathf.Max(0, itemCount);
        }
    }
    //读取道具数量
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        return _itemCounts.TryGetValue(item, out int count) ? count : 0;
    }
    //是否可以使用
    public bool CanUseItem(ItemData item)
    {
        return GetItemCount(item) > 0;
    }
    //使用消耗道具
    private void ConsumeItem(ItemData item)
    {
        if (item == null) return;
        if (!_itemCounts.ContainsKey(item)) return;

        _itemCounts[item]=Mathf.Max(0, _itemCounts[item] - 1);

        OnItemCountChanged?.Invoke(item, _itemCounts[item]);
    }
}



