using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BatteleManager : MonoBehaviour
{
    public List<Character> characters = new List<Character>();
    public bool isActing = false;


    private int tick=0;//时间刻度


    void Start()
    {
        //初始化角色
        //名字，hp，攻击力，速度，行动值
        characters.Add(new Character("Player", 200, 50, 90, 200f));
        characters.Add(new Character("Enemy", 150, 55, 10, 200f));
        Debug.Log("Battle Start!");
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
    }

    void updateActionValues()
    {
        if (isActing) return;
        //减少行动值，当行动值到达0或这者低于0时，触发行动
        foreach (var c in characters)
        {
            if (c.isDead) //没死继续
                continue;
            //速度越快，行动值减少越快
            c.ActionValue -= c.Speed / 0.75f;

            Debug.Log($"{c.Name}的ActionValue={c.ActionValue:F2}");
            

            //找出行动值最小的角色(谁最接近0)
            var nextActor = characters
                .Where(c => !c.isDead)
                .OrderBy(c => c.ActionValue)
                .First();
            //行动值到达0或低于0，且当前没有角色在行动，触发行动
            if (nextActor.ActionValue <= 0 && !isActing)
            {
                StartCoroutine(PerformTrun(nextActor));
            }
            //上面两个逻辑都是为了防止多个角色同时行动
        }
    }
   IEnumerator PerformTrun(Character actor)
    {
        isActing = true;
        actor.isActing = true;
        Debug.Log($"{actor.Name}开始行动！");
        //模拟执行动作
        yield return new WaitForSeconds(1.5f); //等待1秒，模拟行动时间

        //行动完成后恢复行动值
        actor.ActionValue = 200f;
        isActing = false;

        //TODO: 这里可以添加具体的行动逻辑，比如攻击、使用技能等
        /*Debug.Log($"{actor.Name}开始行动！");
        //简单的攻击逻辑，随机选择一个目标进行攻击
        var targets = characters.Where(c => c != actor && !c.isDead).ToList();
        if (targets.Count > 0)
        {
            var target = targets[Random.Range(0, targets.Count)];
            Debug.Log($"{actor.Name}攻击了{target.Name}，造成了{actor.Attack}点伤害！");
            target.Hp -= actor.Attack;
            if (target.Hp <= 0)
            {
                target.isDead = true;
                Debug.Log($"{target.Name}被击败了！");
            }
            else
            {
                Debug.Log($"{target.Name}剩余HP：{target.Hp}");
            }*/
    }
    /*//行动结束，重置行动值
    actor.ActionValue = 200f;
    actor.isActing = false;
    isActing = false;
    yield return null;*/
}


