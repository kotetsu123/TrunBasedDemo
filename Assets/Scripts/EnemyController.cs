using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
   public Character characterData=new Character();  
    void Start()
    {
        characterData.Name = "Goblin";
        characterData.Hp = 50;
        characterData.Attack =5;
        characterData.Speed = 3.0f;
        characterData.ActionValue = 0.0f;

        Debug.Log("EnemyController has rasing "  );
    }

  
}
