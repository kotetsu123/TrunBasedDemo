using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SpawnRequest
{
    public Team team;
    public GameObject prefabs;
    public Character characterData;//Serializable 数据
}


public class BattleSpawner : MonoBehaviour
{
    [SerializeField] private BattleFormation formation;
    [SerializeField] private BattleManager battle;

    private readonly System.Collections.Generic.Queue<SpawnRequest> _enemyReserve = new();

    private void Awake()
    {
        //槽位释放后补位：死一个补一个
        formation.OnSlotChanged += HandleSlotChanged;
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

        var empty = formation.FindFirstEmpty(req.team);
        if (empty >= 0) SpawnInToSlot(req, empty);
        else
        {
            if(req.team==Team.Enemy)_enemyReserve.Enqueue(req);
            //player 通常不会溢出；四人固定，溢出可以直接忽略或者报错
        }
    }
    private void TryFillOneEnemy()
    {
        if (_enemyReserve.Count == 0) return;

        int empty = formation.FindFirstEmpty(Team.Enemy);
        if (empty < 0) return;

        var req=_enemyReserve.Dequeue();
        SpawnInToSlot(req, empty);
    }
    private void SpawnInToSlot(SpawnRequest req, int slotIndex)
    {
        var go = Instantiate(req.prefabs);
        var ctrl = go.GetComponent<BaseController>();
        ctrl.Init(req.characterData);

        bool ok = formation.TryOccupy(req.team, slotIndex, ctrl);
        if (!ok)
        {
            Destroy(go);
            //槽位被抢了；极端情况，敌人回队列
            if (req.team == Team.Enemy) _enemyReserve.Enqueue(req);
            return;
        }
        //站位
        var anchor = formation.GetAnchor(req.team, slotIndex);
        ctrl.transform.position = anchor.position;

        battle.RegisterController(ctrl);
    }

   
}
