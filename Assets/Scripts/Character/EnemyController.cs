using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    
    //[Header("角色数据")]
    [HideInInspector]
    private Character data;
    public Character Data => data;

    private void Start()
    {
        if (data == null)
        {
            data = new Character("Enemy", 150, 55, 10, 200f);
            data.Team = Character.TeamType.Enemy;
            data.isPlayer = false;
            BatteleManager.Instance.RigisterCharacter(data);
            Debug.Log($"[EnemyController]Awake over:{data != null}");
        }

        //Debug.Log("EnemyController has rasing ");
    }



}
