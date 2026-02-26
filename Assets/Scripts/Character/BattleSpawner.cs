using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnRequest
{
    public Team team;
    public GameObject prefabs;
    public Character characterData;//Serializable 数据
}


public class BattleSpawner : MonoBehaviour
{
    [SerializeField] private BattleFormation formation;
    [SerializeField] private BattleManager battle;
    [SerializeField] private List<SpawnRequest> initialEnemies = new();
    [SerializeField] private List<SpawnRequest> initialPlayers = new();
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Canvas worldUICanvas;

    private readonly System.Collections.Generic.Queue<SpawnRequest> _enemyReserve = new();

    private void Awake()
    {
        //槽位释放后补位：死一个补一个
        formation.OnSlotChanged += HandleSlotChanged;
    }
    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnInitial();
        }
    }
    private void OnDestory()
    {
        formation.OnSlotChanged -= HandleSlotChanged;
    }
    private void HandleSlotChanged(Team team, int slotIndex2, BaseController prev, BaseController cur) {
        //只有Enemy slot 变空时补位
        if (team == Team.Enemy && prev != null && cur == null)
        {
            TryFillOneEnemy();
        }
    }
    private void EnqueueOrSpawn(SpawnRequest req)
    {
        if (req.team != Team.Enemy && req.team != Team.Player) return;

        var slotIndex = formation.FindFirstEmpty(req.team);
        if (slotIndex >= 0) SpawnInToSlot(req, slotIndex);
        else
        {
            if(req.team==Team.Enemy)_enemyReserve.Enqueue(req);
            //player 通常不会溢出；四人固定，溢出可以直接忽略或者报错
            return;
        }
        bool ok = SpawnInToSlot(req, slotIndex);
        if (!ok&&req.team==Team.Enemy)
        {
            _enemyReserve.Enqueue(req);
        }
    }
    public void TryFillOneEnemy()
    {
        if (_enemyReserve.Count == 0) return;

        int empty = formation.FindFirstEmpty(Team.Enemy);
        if (empty < 0) return;

        var req=_enemyReserve.Dequeue();
        SpawnInToSlot(req, empty);
    }
    private bool SpawnInToSlot(SpawnRequest req, int slotIndex)
    {
        var go = Instantiate(req.prefabs);
        var ctrl = go.GetComponent<BaseController>();
        ctrl.Init(req.characterData);

        //先注入ui依赖（只对需要worldui的控制器做）
        if(go.TryGetComponent<EnemyController>(out var enemyCtrl))
        {
            enemyCtrl.InjectWorldUICanvas(worldUICanvas);
        }
        //占位
        bool ok = formation.TryOccupy(req.team, slotIndex, ctrl);
        if (!ok)
        {
            Destroy(go);
            //下面是敌人逻辑，为了改成通用逻辑进行注释，并且固定在EnqueueOrSpawn方法当中
            /*//槽位被抢了；极端情况，敌人回队列 
            if (req.team == Team.Enemy) _enemyReserve.Enqueue(req);
            return;*/
            return false;
        }
        //站位
        var anchor = formation.GetAnchor(req.team, slotIndex);
        ctrl.transform.position = anchor.position;
        //站位完成后进行初始化血条
        if (enemyCtrl != null)
        {
            enemyCtrl.EnsureHpBarInitialized();
        }
 
        //注册进战斗/时间轴
        battle.RegisterController(ctrl);

        return true;
    }
    public void SpawnInitial()
    {
        //Player: 按顺序塞（0-3）
        for(int i=0; i < initialPlayers.Count; i++)
        {
            if (i >= 4) break;
            SpawnInToSlot(initialPlayers[i], i);
        }
        //Enemy:
        foreach(var req in initialEnemies)
        {
            EnqueueOrSpawn(req);
        }
    }
   public void SpawnPlayerInitial(List<SpawnRequest> playerTeam)
    {
        for(int i = 0; i < playerTeam.Count; i++)
        {
            SpawnInToSlot(playerTeam[i], i);
        }
    }
}
