using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TragetCircleController : MonoBehaviour
{
    [SerializeField] private BattleManager battle;
    [SerializeField] private TargetCircle targetCirclePrefab;//直接引用带脚本的prefabs
    [SerializeField] private Transform circleParent;

    private bool _playerTurn;
    private TargetCircle _instance;//圈圈实例 
    private BaseController _target;//被选中的目标

    
    void Awake()
    {
        if (battle != null)
        {
            battle.OnTargetChanged += HandleTargetChanged;
            battle.OnInputStateChanged += HandleInputStateChanged;
        }
    }

    private void HandleInputStateChanged(bool playerTurn)
    {
        _playerTurn = playerTurn;
        Apply();
        
    }
    private void HandleTargetChanged(BaseController target)
    {
        _target = target;
        EnsureInstance();
        Apply();
    }
  
    private void OnDestroy()
    {
        if (battle != null)
        { battle.OnTargetChanged -= HandleTargetChanged;
            battle.OnInputStateChanged -= HandleInputStateChanged;
        }
    }
    private void EnsureInstance()
    {
        if (_instance != null) return;
        if (targetCirclePrefab == null) return;

        _instance = Instantiate(targetCirclePrefab, circleParent != null ? circleParent : transform);
        _instance.Attach(null);//默认隐藏
    }
    private void Apply()
    {
        if (_instance == null) return;

        //规则：玩家回合+有目标 才显示
        if (_playerTurn && _target != null)
            _instance.Attach(_target.transform);
        else
            _instance.Attach(null);
    }
}
