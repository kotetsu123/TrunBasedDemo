using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TragetCircleController : MonoBehaviour
{
    [SerializeField] private BattleManager battle;
    [SerializeField] private GameObject targetCirclePrefab;

    private GameObject _instance;
    // Start is called before the first frame update
    void Start()
    {
        if (battle != null)
            battle.OnTargetChanged += HandleTargetChanged;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void HandleTargetChanged(BaseController target)
    {
        if (target == null)
        {
            if(_instance!=null)
                _instance.SetActive(false);
            return;
        }
        if (_instance == null)
            _instance = Instantiate(targetCirclePrefab);

        _instance.SetActive(true);
        _instance.transform.position=target.transform.position;
    }
    private void OnDestroy()
    {
        if(battle !=null)
            battle.OnTargetChanged -= HandleTargetChanged;
    }
}
