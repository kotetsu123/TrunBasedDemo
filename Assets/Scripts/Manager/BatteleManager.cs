using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BatteleManager : MonoBehaviour
{
    public static BatteleManager Instance;

    public List<Character> characters = new List<Character>();
    public bool isActing = false;
    public bool battleEnded = false;

    public PlayerController player;
    public EnemyController enemy;


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

    public void RigisterCharacter(Character c)
    {
        characters.Add(c);
        Debug.Log($"[BattleManager] Rigister Charcter:{c.Name}");
        
    }
    void updateActionValues()
    {
        if (isActing) return;
        //减少行动值，当行动值到达0或这者低于0时，触发行动
        foreach (var c in characters)
        {
            if (c.isDead) //死亡角色不行动
                continue;
            //速度越快，行动值减少越快
            c.ActionValue -= c.Speed / 0.75f;

           // Debug.Log($"{c.Name}的ActionValue={c.ActionValue:F2}");


            //找出行动值最小的角色(谁最接近0)
            var nextActor = characters
                .Where(c => !c.isDead)
                .OrderBy(c => c.ActionValue)
                .First();
            //行动值到达0或低于0，且当前没有角色在行动，触发行动
            if (nextActor.ActionValue <= 0 && !isActing)
            {
                StartCoroutine(PerformTrun(nextActor));
                break;
            }
            //上面两个逻辑都是为了防止多个角色同时行动
        }
    }
    /* IEnumerator RigisterDone()
     {
         yield return new WaitUntil(() => characters.Count >= 2);
         Debug.Log("[BattleManager] Rigister Done!");

     }*/
    IEnumerator PerformTrun(Character actor)
    {
        //战斗结束终止行动
        if (battleEnded)
            yield break;

        isActing = true;
        actor.isActing = true;
        Debug.Log($"{actor.Name}开始行动！");
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
            var target = characters.FirstOrDefault(c => c.Team != actor.Team && !c.isDead);
            if (target != null)
            {
                target.TakeDamage(actor.Attack);
                CheckBattleEnd(actor, target);
            }
            yield return new WaitForSeconds(1f);
        }
        //行动完成后恢复行动值
        actor.ActionValue = 200f;
        isActing = false;

        Debug.Log($"{actor.Name}结束行动！");

    }
    IEnumerator WaitForPlayerAction(Character actor)
    {
        bool actionChosen = false;
        Debug.Log("press the space key attack the enemy");
        while (!actionChosen)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var target = characters.FirstOrDefault(c => c.Team != actor.Team && !c.isDead);
                if (target != null)
                {
                    target.TakeDamage(actor.Attack);
                    //Debug.LogFormat($"{actor.Name} attack the {target.Name} rise {actor.Attack} damage");
                    Debug.Log($"{actor.Name} attack the {target.Name} rise {actor.Attack} damage");
                    CheckBattleEnd(actor, target);
                }
                actionChosen = true;
            }

            yield return null;
        }
    }
    void CheckBattleEnd(Character attacker, Character target)
    {
        if (target.Hp <= 0)
        {
            battleEnded = true;
            Debug.Log($"Battle Finish!");
        }
    }
}



