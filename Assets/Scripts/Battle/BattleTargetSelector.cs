using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTargetSelector : MonoBehaviour
{
    //点击检测相关
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask enemyClickMask;//敌人点击层
    [SerializeField] private float clickMaxDistance = 200f;//点击检测最大距离

    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleFormation formation;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    public void HandleTargetSelectionInput(BaseController actor)
    {
        if (actor == null || actor.data == null) return;
        if (actor.data.Team != Team.Player) return;

        //鼠标点击 切换目标
        HandleMouseClickSelect(actor);
        //Tab切换
        CycleEnemyTarget(actor);
        // A/D 切换
        if (Input.GetKeyDown(KeyCode.A))
            SelectEnemyLeftRight(-1);
        if (Input.GetKeyDown(KeyCode.D))
            SelectEnemyLeftRight(1);

        //如果目标死了/无效了，自动选一个目标
        if (!IsValidEnemyTarget(actor, battleManager.CurrentTarget))
            AutoPickTargetIfNeeded(actor);
    }
    public bool IsValidEnemyTarget(BaseController actor, BaseController target)
    {
        if (actor == null || actor.data == null) return false;
        if (target == null || target.data == null) return false;
        if (target.data.isDead || target.isDead) return false;
        return target.data.Team != actor.data.Team;
    }
  
    public void AutoPickTargetIfNeeded(BaseController actor)
    {
        if (IsValidEnemyTarget(actor,battleManager.CurrentTarget)) return;//当前目标有效，不需要自动选
        //从formation获取按照槽位优先级排序的存活敌人
        var list = formation.GetAliveEnemiesInPreferredOrder();
        battleManager.SetCurrentTarget(list.Count > 0 ? list[0] : null);
    }
    //Tab键切换目标
    private void CycleEnemyTarget(BaseController attcker)
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        var list = formation.GetAliveEnemiesInPreferredOrder();
        if (list.Count == 0)
        {
           battleManager.SetCurrentTarget(null);
            return;
        }
        int idx = list.IndexOf(battleManager.CurrentTarget);
        idx = (idx + 1) % list.Count;
      battleManager.  SetCurrentTarget(list[idx]);
    }
    // A/D键左右切换目标
    private void SelectEnemyLeftRight(int direction)
    {
        if (battleManager.CurrentActor == null || battleManager.CurrentActor.data == null) return;
        if (battleManager.CurrentActor.data.Team != Team.Player) return;

        var list = formation.GetAliveEnemiesInSpatialOrder();

       
        if (list.Count == 0)
        {
            battleManager.SetCurrentTarget(null);
            return;
        }
       
        int index = list.IndexOf(battleManager.CurrentTarget);
        

        int next = Mathf.Clamp(index + direction, 0, list.Count - 1);

        if (next != index)
            battleManager.SetCurrentTarget(list[next]);

    }
    //点击检测，切换目标
    private void HandleMouseClickSelect(BaseController actor)
    {
        if (mainCamera == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, clickMaxDistance, enemyClickMask))
        {

            var targetable = hit.collider.GetComponentInParent<Targetable>();
            if (targetable != null && IsValidEnemyTarget(actor, targetable.controller))
            {
                battleManager.SetCurrentTarget(targetable.controller);
            }
        }
    }
}
