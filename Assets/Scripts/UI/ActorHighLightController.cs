using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorHighLightController : MonoBehaviour
{
    [SerializeField] private BattleManager battle;
    [SerializeField] private ActorHighLight highLightPrefab;
    [SerializeField] private Transform highLightParent;

    [SerializeField] private Color playerColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.35f, 0.35f);

    private ActorHighLight _instance;
    private BaseController _currentActor;

    private void Awake()
    {
        if (battle != null)
        {
            battle.OnCurrentActorChanged += HandleCurrentActorChanged;
        }
    }
    private void OnDestroy()
    {
        if (battle != null)
        {
            battle.OnCurrentActorChanged-= HandleCurrentActorChanged;
        }
    }
    private void HandleCurrentActorChanged(BaseController actor)
    {
        _currentActor= actor;
        EnsureInstance();
        Apply();
    }
    private void EnsureInstance()
    {
        if (_instance != null) return;
        if (highLightPrefab == null) return;

        _instance = Instantiate(highLightPrefab, highLightParent != null ? highLightParent : transform);
        _instance.Attach(null);
    }
    private void Apply()
    {
        if (_instance == null) return;

        if (_currentActor == null || _currentActor.data == null)
        {
            _instance.Attach(null);
            return;
        }
        _instance.Attach(_currentActor.transform);

        if (_currentActor.data.Team == Team.Player)
            _instance.SetColor(playerColor);
        else if(_currentActor.data.Team==Team.Enemy)
            _instance.SetColor(enemyColor);
    }
}
