using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleTargetSelector : MonoBehaviour
{
    //点击检测相关
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask enemyClickMask;//敌人点击层
    [SerializeField] private LayerMask allyClickMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float clickMaxDistance = 200f;//点击检测最大距离

    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleFormation formation;

    private readonly List<RaycastResult> _pointerHits = new List<RaycastResult>();

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    public void HandleTargetSelectionInput(BaseController actor,SkillTargetType targetType)
    {
        if (battleManager == null || formation == null) return;
        if (battleManager.battleEnded) return;
        if (TutorialPanelController.IsTutorialActive || Time.timeScale <= 0f) return;
        if (actor == null || actor.data == null) return;
        if (actor.data.Team != Team.Player) return;

        if (targetType == SkillTargetType.Self)
        {
            if (battleManager.CurrentTarget != actor)
                battleManager.SetCurrentTarget(actor);

            return;
        }
           
        if (targetType == SkillTargetType.EnemySingle)
        {
            //鼠标点击 切换目标
            HandleMouseClickSelectEnemy(actor);
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

        if (targetType == SkillTargetType.AllySingle || targetType == SkillTargetType.AllyDeadSingle)
        {
            HandleAllySelectionInput(actor, targetType);
            return;
        }
       
    }
    public bool IsValidEnemyTarget(BaseController actor, BaseController target) { 
        if (actor == null || actor.data == null) return false;
        if (target == null || target.data == null) return false;
        if (target.data.isDead || target.isDead) return false;
        
        return target.data.Team != actor.data.Team;
    }
    public bool IsValidAllyTarget(BaseController actor, BaseController target) { 
        if (actor == null || actor.data == null) return false;
        if (target == null || target.data == null) return false;
        if (target.data.isDead || target.isDead) return false;
        if (target.data.Hp <= 0) return false;

        return target.data.Team == actor.data.Team;
    }
    public bool IsValidDeadAllyTarget(BaseController actor, BaseController target) {
        if (actor == null || actor.data == null || target == null) return false;
        if (target.data == null) return false;

        bool sameTeam=actor.data.Team==target.data.Team;
        bool isDead = target.data.isDead || target.isDead || target.data.Hp <= 0;
        return sameTeam && isDead;
    }
    public bool IsValidTarget(BaseController actor, BaseController target,SkillTargetType targetType)
    {
        if(actor==null||target==null||target.data==null) return false;
        switch (targetType)
        {
            case SkillTargetType.EnemySingle:
                return IsValidEnemyTarget(actor, target);
            case SkillTargetType.AllySingle:
                return IsValidAllyTarget(actor, target);
            case SkillTargetType.AllyDeadSingle:
                return IsValidDeadAllyTarget(actor, target);
            case SkillTargetType.Self:
                return target == actor;
            default:
                return false;
        }
        
    }
  
    public void AutoPickTargetIfNeeded(BaseController actor)
    {
        if (IsValidEnemyTarget(actor,battleManager.CurrentTarget)) return;//当前目标有效，不需要自动选
        //从formation获取按照槽位优先级排序的存活敌人
        var list = formation.GetAliveEnemiesInPreferredOrder();
        battleManager.SetCurrentTarget(list.Count > 0 ? list[0] : null);
        battleManager.SetPreviewTarget(list.Count > 0 ? list[0] : null);
    }
    public void AutoPickAllyTargetIfNeeded(BaseController actor)
    {
        if (IsValidAllyTarget(actor, battleManager.CurrentTarget)) return;

        var list = GetAllyTargets(actor, SkillTargetType.AllySingle);
        SetAllyTarget(list.Count > 0 ? list[0] : null);
    }
    public void AutoPickDeadAllyTargetIfNeeded(BaseController actor)
    {
        if (IsValidDeadAllyTarget(actor, battleManager.CurrentTarget)) return;

        var list = GetAllyTargets(actor, SkillTargetType.AllyDeadSingle);
        SetAllyTarget(list.Count > 0 ? list[0] : null);
    }
    //Tab键切换目标（敌人）
    private void CycleEnemyTarget(BaseController attcker)
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        var list = formation.GetAliveEnemiesInPreferredOrder();
        if (list.Count == 0)
        {
           battleManager.SetCurrentTarget(null);
            battleManager.SetPreviewTarget(null);
            return;
        }
        int idx = list.IndexOf(battleManager.CurrentTarget);
        idx = (idx + 1) % list.Count;
      battleManager.SetCurrentTarget(list[idx]);
         battleManager.SetPreviewTarget(list[idx]);
    }
    //（玩家/活）
    private void CycleAllyTarget(BaseController attcker)
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        var list = GetAllyTargets(attcker, SkillTargetType.AllySingle);
        if (list.Count == 0)
        {
            SetAllyTarget(null);
            return;
        }
        int idx = list.IndexOf(battleManager.CurrentTarget);
        idx = (idx + 1) % list.Count;
        SetAllyTarget(list[idx]);
    }
    //（玩家/死）
    private void CycleDeadAllyTarget(BaseController attacker)
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        var list = GetAllyTargets(attacker, SkillTargetType.AllyDeadSingle);
        if (list.Count == 0)
        {
            SetAllyTarget(null);
            return;
        }
        int idx = list.IndexOf(battleManager.CurrentTarget);
        idx=(idx+1)%list.Count;
        SetAllyTarget(list[idx]);
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
            battleManager.SetPreviewTarget(null);
            return;
        }
       
        int index = list.IndexOf(battleManager.CurrentTarget);
        

        int next = Mathf.Clamp(index + direction, 0, list.Count - 1);

        if (next != index)
        {
            battleManager.SetCurrentTarget(list[next]);
            battleManager.SetPreviewTarget(list[next]);
        }
           


    }
    //点击检测，切换目标
    private void HandleMouseClickSelectEnemy(BaseController actor)
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
                battleManager.SetPreviewTarget(targetable.controller);
            }
        }
    }
    private void HandleAllySelectionInput(BaseController actor, SkillTargetType targetType)
    {
        HandleMouseClickSelectAlly(actor, targetType);

        if (targetType == SkillTargetType.AllyDeadSingle)
            CycleDeadAllyTarget(actor);
        else
            CycleAllyTarget(actor);

        if (Input.GetKeyDown(KeyCode.A))
            SelectAllyLeftRight(actor, targetType, -1);
        else if (Input.GetKeyDown(KeyCode.D))
            SelectAllyLeftRight(actor, targetType, 1);

        if (targetType == SkillTargetType.AllyDeadSingle)
            AutoPickDeadAllyTargetIfNeeded(actor);
        else
            AutoPickAllyTargetIfNeeded(actor);
    }

    private List<BaseController> GetAllyTargets(BaseController actor, SkillTargetType targetType)
    {
        var list = targetType == SkillTargetType.AllyDeadSingle
            ? formation.GetDeadPlayersInSlotOrder()
            : formation.GetPlayersInSlotOrder();
        list.RemoveAll(target => !IsValidTarget(actor, target, targetType));
        return list;
    }

    private void SetAllyTarget(BaseController target)
    {
        if (battleManager.CurrentTarget == target && battleManager.PreviewTarget == target)
            return;

        battleManager.SetCurrentTarget(target);
        battleManager.SetPreviewTarget(target);
    }

    private void SelectAllyLeftRight(BaseController actor, SkillTargetType targetType, int direction)
    {
        var list = GetAllyTargets(actor, targetType);
        if (list.Count == 0)
        {
            SetAllyTarget(null);
            return;
        }

        // Use screen order so A/D still matches left/right when the camera changes sides.
        if (mainCamera != null)
            list = list.OrderBy(target => mainCamera.WorldToScreenPoint(target.transform.position).x).ToList();

        int index = list.IndexOf(battleManager.CurrentTarget);
        int next = index < 0 ? 0 : Mathf.Clamp(index + direction, 0, list.Count - 1);
        if (next != index)
            SetAllyTarget(list[next]);
    }

    private void HandleMouseClickSelectAlly(BaseController actor, SkillTargetType targetType)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null)
        {
            _pointerHits.Clear();
            var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            EventSystem.current.RaycastAll(pointer, _pointerHits);
            if (_pointerHits.Count > 0)
            {
                // Only the frontmost HUD may select a target; other UI blocks world clicks.
                var hud = _pointerHits[0].gameObject.GetComponentInParent<PlayerHudItem>();
                if (hud != null && IsValidTarget(actor, hud.Bount, targetType))
                    SetAllyTarget(hud.Bount);
                return;
            }
        }

        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, clickMaxDistance, allyClickMask))
        {
            var targetable = hit.collider.GetComponentInParent<Targetable>();
            var target = targetable != null && targetable.controller != null
                ? targetable.controller
                : hit.collider.GetComponentInParent<BaseController>();
            if (IsValidTarget(actor, target, targetType))
                SetAllyTarget(target);
        }
    }
}
