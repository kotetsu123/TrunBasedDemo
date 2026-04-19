using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCameraDirector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BattleManager battle;

    [Header("Global Shots")]
    [SerializeField] private Transform defaultBattleShot;
    [SerializeField] private Transform playerGroupShot;
    [SerializeField]private Transform enemyGroupShot;

    [Header("Move Settings")]
    [SerializeField]private float moveDuration = 0.35f;
    [SerializeField]private AnimationCurve moveCurve=AnimationCurve.EaseInOut(0, 0, 1, 1);

    //特殊镜头点
    [SerializeField] private Transform interactionShotPoint;

    private Coroutine _cameraMoveRoutine;
    private bool _isLocked;

    public bool IsLocked => _isLocked;

    private void OnEnable()
    {
        if(battle!=null)
            battle.OnCurrentActorChanged += HandleCurrentActorChanged;
        else
        {
            Debug.LogWarning("[Camera] battleManager is null in OnEnable");
        }
    }
    private void OnDisable()
    {
        if(battle!=null)
            battle.OnCurrentActorChanged -= HandleCurrentActorChanged;
    }
    private void Start()
    {
        //SnapToShot(defaultBattleShot);
    }
    public void LockCamera()
    {
               _isLocked = true;
    }
    public void UnlockCamera()
    {
        _isLocked = false;
    }
    private void HandleCurrentActorChanged(BaseController actor)
    {
        Debug.Log($"[Camera] HandleCurrentActorChanged -> {actor?.data?.Name}");
        if (_isLocked)
            return;

        if (actor == null) return;
        FocusActorTurnShot(actor);
    }
    public void FocusActorTurnShot(BaseController actor)
    {
       if(actor==null) return;

        var anchor = actor.GetComponent<BattleCameraAnchor>();
        if(anchor!=null&&anchor.TrunCameraPoint!=null)
        {
            MoveToShot(anchor.TrunCameraPoint);
        }
    }
    public  void FocusTargetHitShot(BaseController target)
    {
        if (target == null) return;
        
        var anchor=target.GetComponent<BattleCameraAnchor>();
        if(anchor!=null&&anchor.HitCameraPoint!=null)
        {
            MoveToShot(anchor.HitCameraPoint);
        }
    }
    //双方镜头//在这个版本当中，会存在，如果是玩家攻击敌人，则镜头偏向玩家；如果是敌人攻击玩家，则镜头偏向敌人
    //所以需要需要规定一个 规则： 镜头偏向玩家
    //所以我们需要在外面封装一个新的方法，因此此方法为其底层函数
    public void FocusInteractionShot(BaseController attacker,BaseController target)
    {
        if(attacker==null||target==null||targetCamera==null) return;
        
        Vector3 attackerPos=attacker.transform.position;
        Vector3 targetPos=target.transform.position;

        //中点
        Vector3 mid = (attackerPos + targetPos) * 0.5f;
        //攻击方向
        Vector3 dir = (targetPos - attackerPos).normalized;
        //偏方向
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

        //调整参数
        float backDistance = 3.0f;//往后退多少
        float  sideOffset = 1.5f;//往侧面偏移多少
        float height = 1.0f;//高度

        //计算摄像机位置
        Vector3 camPos=
            mid
            -dir* backDistance 
            + side * sideOffset
            + Vector3.up * height;
        //看向点（略微偏向target的上半身）
        Vector3 lookAt=mid+ Vector3.up * 1.2f;

        
        interactionShotPoint.transform.position= camPos;
        interactionShotPoint.transform.rotation= Quaternion.LookRotation(lookAt - camPos);

        MoveToShot(interactionShotPoint.transform);      
    }
    //双方镜头，封装规则：镜头偏向玩家
    public void FocusPlayerSideInteractionSHot(BaseController actor, BaseController target)
    {
        if (actor == null || target == null)
            return;
        if (actor.data == null || target.data == null)
            return;
        //规则：
        //只要玩家在这组交互里，镜头就偏向玩家
        if (actor.data.Team == Team.Player)
        {
            //玩家出手：正常顺序
            FocusInteractionShot(actor, target);
            return;
        }
        if (target.data.Team == Team.Player)
        {
            //敌人出手，镜头偏向玩家，所以颠倒顺序
            FocusInteractionShot(target, actor);
             return;
        }
        //都不是玩家，则默认规则
        FocusInteractionShot(actor, target);
    }
    public void FocusPlayerGroup()
    {
        MoveToShot(playerGroupShot);
    }
    public void FocusEnemyGroup()
    {
        MoveToShot(enemyGroupShot);
    }
    public void FocusDefault()
    {
        MoveToShot(defaultBattleShot);
    }
    public void SnapToShot(Transform shot)
    {
        if (targetCamera == null || shot == null) return;

        targetCamera.transform.position = shot.position;
        targetCamera.transform.rotation = shot.rotation;
    }
    public void MoveToShot(Transform shot)
    {
        if(targetCamera==null||shot==null) return;

        if (_cameraMoveRoutine != null)
            StopCoroutine(_cameraMoveRoutine);

        _cameraMoveRoutine = StartCoroutine(MoveRoutine(shot));
    }
    private IEnumerator MoveRoutine(Transform shot)
    {
        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

        Vector3 targetPos = shot.position;
        Quaternion targetRot = shot.rotation;

        float elasped = 0f;
        while (elasped < moveDuration)
        {
            elasped += Time.deltaTime;
            float t = Mathf.Clamp01(elasped / moveDuration);
            t = moveCurve.Evaluate(t);

            targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        targetCamera.transform.position = targetPos;
        targetCamera.transform.rotation = targetRot;
        _cameraMoveRoutine = null;
    }
}

