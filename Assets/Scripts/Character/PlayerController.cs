using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    //[Header("角色数据")]
    [HideInInspector]
    private Character data;

    public Character Data => data;

    private void Start()
    {
        if (data == null)
        {
            data = new Character("Player", 200, 50, 90, 200f);
            BatteleManager.Instance.RigisterCharacter(data);
            Debug.Log($"[PlayerController]Awake over:{data != null}");
        }
       
        //Debug.Log("PlayerController has rasing "  );
    }
}


