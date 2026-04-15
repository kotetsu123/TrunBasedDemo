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

