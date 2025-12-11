using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BatteleManager : MonoBehaviour
{
    public static BatteleManager Instance;

   // public List<Character> characters = new List<BaseController>();
    public List<BaseController> controllers = new List<BaseController>();

    public bool isActing = false;
    public bool battleEnded = false;
    public bool isBattle = true;

    public PlayerController player;
    public EnemyController enemy;
    public GameObject timeLineIconPrefab;
    public RectTransform actionBarPanel;
    public List<RectTransform> slots;

    private Dictionary<BaseController, TimeLineIcon> timeLineIcons = new Dictionary<BaseController, TimeLineIcon>();
    private int tick = 0;//时间刻度

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

    public void RigisterCharacter(BaseController c)
    {
        controllers.Add(c);
        Debug.Log($"[BattleManager] Rigister Charcter:{c.data.Name}");
        
    }
    public void RegisterTimeLineIcon(BaseController character)
    {
        var iconobj = Instantiate(timeLineIconPrefab, actionBarPanel);
        var icon=iconobj.GetComponent<TimeLineIcon>();
        icon.Bind(character,actionBarPanel);

        timeLineIcons.Add(character, icon);
    }
    void updateActionValues()
    {
        if (isActing) return;

        bool shouldCheckForTurn = false;

        foreach (var c in controllers)
        {
            if (c.isDead) continue;

            // 增加行动值，而不是减少（这样更符合时间轴逻辑）
            c.data.ActionValue += c.data.Speed * Time.deltaTime * 10f; // 调整系数

            // 如果超过阈值，标记需要检查回合
            if (c.data.ActionValue >= c.data.maxActionValue)
            {
                c.data.ActionValue = c.data.maxActionValue;
                shouldCheckForTurn = true;
            }

            // 更新UI位置
            if (timeLineIcons.ContainsKey(c))
            {
                timeLineIcons[c].UpdatePosition();
            }
        }

        // 只在有角色到达阈值时检查回合
        if (shouldCheckForTurn && !isActing)
        {
            // 找出行动值最高的角色（谁先到达阈值）
            var nextActor = controllers
                .Where(c => !c.isDead && c.data.ActionValue >= c.data.maxActionValue)
                .OrderByDescending(c => c.data.Speed) // 速度快的优先
                .ThenByDescending(c => c.data.ActionValue) // 行动值高的优先
                .FirstOrDefault();

            if (nextActor != null)
            {
                StartCoroutine(PerformTurn(nextActor));
            }
        }
    }
    /* IEnumerator RigisterDone()
     {
         yield return new WaitUntil(() => characters.Count >= 2);
         Debug.Log("[BattleManager] Rigister Done!");

     }*/
    IEnumerator PerformTurn(BaseController actor)
    {
        //战斗结束终止行动
        if (battleEnded)
            yield break;

        isActing = true;
        actor.data.isActing = true;
        Debug.Log($"{actor.data.Name}开始行动！");
        //新增立即重置行动值，避免多次触发
        actor.data.ActionValue = 0f;
        timeLineIcons[actor].UpdatePosition();

        //模拟执行动作
        yield return new WaitForSeconds(1.5f); //等待1秒，模拟行动时间
                                               //
        if (actor.isPlayer)
        {
            
            //等待玩家输入
            Debug.Log("等待玩家输入指令...");
            yield return StartCoroutine(WaitForPlayerAction(actor));
           
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
        actor.data.ActionValue = 200f;
        //isActing = false;
        //actor.data.ActionValue = actor.data.startValue;      
       // actor.data.isActing = false;
       // timeLineIcons[actor].UpdatePosition();
      
        actor.data.isActing = false;
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
        }
    }
    
}
