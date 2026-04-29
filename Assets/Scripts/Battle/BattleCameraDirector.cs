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
    [SerializeField] private Transform enemyGroupShot;

    [Header("Move Settings")]
    [SerializeField]private float moveDuration = 0.35f;
    [SerializeField]private AnimationCurve moveCurve=AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField]private float startSequenceFirstDelay = 1.5f;
    [SerializeField]private float startSequenceSecondDelay = 1.0f;

    //特殊镜头点
    [SerializeField] private Transform interactionShotPoint;

    private BaseController _laseActor;
    private BaseController _lastTarget;

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

        if (actor == null || actor.data == null) return;
        if (actor.data.Team == Team.Player)
        {
            FocusBattlePreviewShot(_laseActor, _lastTarget);           
        }
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
    public IEnumerator PlayBattleStartSequence(BaseController player,BaseController target)
    {
         LockCamera();

        FocusEnemyGroup();
        yield return new WaitForSeconds(startSequenceFirstDelay);
        
        FocusPlayerSideInteractionShot(player, target,2.0f);
        yield return new WaitForSeconds(startSequenceSecondDelay);
         
       UnlockCamera();
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
    public void FocusInteractionShot(BaseController attacker,BaseController target,float?durationOverride=null)
    {
        if(attacker==null||target==null||targetCamera==null) return;
        
        Vector3 attackerPos=attacker.transform.position;
        Vector3 targetPos=target.transform.position;

     
        //攻击方向
        Vector3 dir = (targetPos - attackerPos).normalized;
        //偏方向
        Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
        //相机离actor 身后多远
        float backDistance = 3.0f;
        //相机离actor 侧面多远
        float sideOffset = 1.0f;
        //相机离actor 多高
        float height = 1.0f;
        
        //计算摄像机位置
        Vector3 camPos=
            attackerPos
            -dir* backDistance 
            -side * sideOffset
            + Vector3.up * height;
        

        Vector3 lookAt = targetPos;//
     
        interactionShotPoint.transform.position= camPos;
        interactionShotPoint.transform.rotation= Quaternion.LookRotation(lookAt - camPos);

        MoveToShot(interactionShotPoint.transform,durationOverride);      
    }
    //双方镜头，封装规则：镜头偏向玩家
    public void FocusPlayerSideInteractionShot(BaseController actor, BaseController target,float?durationOverride=null)
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
            FocusInteractionShot(actor, target, durationOverride);
            return;
        }
        if (target.data.Team == Team.Player)
        {
            //敌人出手，镜头偏向玩家，所以颠倒顺序
            FocusInteractionShot(target, actor, durationOverride);
             return;
        }
        //都不是玩家，则默认规则
        FocusInteractionShot(actor, target,durationOverride);
    }
    public void FocusPlayerSideTargetPreviewShot(BaseController actor,BaseController target, float? durationOverride = null)
    {
        if (actor == null || target == null || targetCamera == null || interactionShotPoint == null)
            return;
        if(actor.data==null||target.data==null) 
            return;

        BaseController previewActor = actor;
        BaseController previewTarget = target;

        //规则：统一偏向玩家
        if(actor.data.Team==Team.Enemy&&target.data.Team==Team.Player)
        {
            previewActor = target;
            previewTarget = actor;
        }
        Vector3 actorPos= previewActor.transform.position;
        Vector3 targetPos= previewTarget.transform.position;

        //攻击方向
        Vector3 dir=(targetPos-actorPos).normalized;
        //偏方向
        Vector3 side=Vector3.Cross(Vector3.up,dir).normalized;

        //浏览参数
        float backDistance=3.0f;
        float sideOffset=2.5f;
        float height=2f;

        Vector3 camPos=
            actorPos
            -dir* backDistance
            +side*sideOffset
            + Vector3.up*height;

        Vector3 lookAt=targetPos;
        interactionShotPoint.position= camPos;
        interactionShotPoint.rotation= Quaternion.LookRotation(lookAt-camPos);

        MoveToShot(interactionShotPoint, durationOverride);

    }
    public void FocusBattlePreviewShot(BaseController actor,BaseController target)
    {
        if (actor == null || target == null) return;

       
        FocusPlayerSideTargetPreviewShot(actor, target);
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
    public void MoveToShot(Transform shot,float?durationOverride=null)
    {
        if(targetCamera==null||shot==null) return;

        if (_cameraMoveRoutine != null)
            StopCoroutine(_cameraMoveRoutine);

        float duration= durationOverride ?? moveDuration;

        _cameraMoveRoutine = StartCoroutine(MoveRoutine(shot,duration));
    }
    private IEnumerator MoveRoutine(Transform shot,float duration)
    {
        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

        Vector3 targetPos = shot.position;
        Quaternion targetRot = shot.rotation;

        float elasped = 0f;
        while (elasped < duration)
        {
            elasped += Time.deltaTime;
            float t = Mathf.Clamp01(elasped / duration);
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

