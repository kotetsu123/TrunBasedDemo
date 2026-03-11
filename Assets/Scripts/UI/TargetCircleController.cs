using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetCircleController : MonoBehaviour
{
    [SerializeField] private BattleManager battle;
    [SerializeField] private TargetCircle targetCirclePrefab;//直接引用带脚本的prefabs
    [SerializeField] private Transform circleParent;

    //private bool _playerTurn;
    private bool _canTargetSelect;
    private TargetCircle _instance;//圈圈实例 
    private BaseController _target;//被选中的目标//_lastTarget

    
    void Awake()
    {
        if (battle != null)
        {
            battle.OnTargetChanged += HandleTargetChanged;
            battle.OnInputStateChanged += HandleInputStateChanged;
        }
    }

    private void HandleInputStateChanged(bool canTargetSelect)
    {
        _canTargetSelect = canTargetSelect;
        if(!canTargetSelect)
            
        Debug.Log($"[Circle] InputStateChanged playerTurn={canTargetSelect} actorTeam={battle?.CurrentActor?.data?.Team}");
        _canTargetSelect = canTargetSelect;
        Apply();
        
    }
    private void HandleTargetChanged(BaseController target)
    {
        Debug.Log($"[Circle] TargetChanged target={(target ? target.data?.Name : "null")} canTargetSelect={_canTargetSelect}");
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
        if (_canTargetSelect && _target != null)
            _instance.Attach(_target.transform);
        else
            _instance.Attach(null);
    }
}
