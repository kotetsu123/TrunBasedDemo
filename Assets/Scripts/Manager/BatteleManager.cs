using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BatteleManager : MonoBehaviour
{
    public static BatteleManager Instance;

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

    /*public void RegisterTimeLineIcon(BaseController character)
{

}*/
    void updateActionValues()
    {
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
        Debug.Log("Ordered:" + string.Join(",", ordered.Select(x => x.data.Name)));
        //更新ui
        UpdateTimeLineUI(ordered);
        //更新ui
        var nextActor= ordered.FirstOrDefault();
        if(nextActor != null && nextActor.data.ActionValue <= 0 && !isActing)
        {
            StartCoroutine(PerformTrun(nextActor));
        }
       /* foreach (var c in controllers)
        {
            UpdateTimeLineUI(ordered);
            var nextActor = ordered.First();
            //行动值到达0或低于0，且当前没有角色在行动，触发行动
            if (nextActor.data.ActionValue <= 0 && !isActing)
            {
                StartCoroutine(PerformTrun(nextActor));

            }
        }*/
        //上面两个逻辑都是为了防止多个角色同时行动
    }

    /* IEnumerator RigisterDone()
     {
         yield return new WaitUntil(() => characters.Count >= 2);
         Debug.Log("[BattleManager] Rigister Done!");

     }*/
    IEnumerator PerformTrun(BaseController actor)
    {
        //战斗结束终止行动
        if (battleEnded)
            yield break;

        isActing = true;
        actor.data.isActing = true;
        Debug.Log($"{actor.data.Name}开始行动！");
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
        /*for (int i = 0; i < ordered.Count&&i<slots.Count; i++)
        {
            var c = ordered[i];
            var icons = timeLineIcons[c];
            var iconRect = icons.GetComponent<RectTransform>();

            iconRect.SetParent(slots[i], false);
            iconRect.anchoredPosition = Vector2.zero;//居中/归零
            iconRect.localScale = Vector3.one;

            //RectTransform slotRect = slots[i];
            //iconRect.position = slotRect.position;
            //icons.transform.position=slots[i].position;
        }*/
        for(int i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            if (!timeLineIcons.TryGetValue(c, out var icon)) continue;

            //直接把prefab 变成actionbarpanel 的子物体

            icon.transform.SetParent(actionBarPanel, false);

            //改兄弟顺序。让layoutGroup 自动重排
            icon.transform.SetSiblingIndex(i); 

            //强制刷新布局
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionBarPanel);
        }
    }
}



