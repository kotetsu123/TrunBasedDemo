using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Character characterData=new Character();

    private void Start()
    {
        characterData.Name = "Player";
        characterData.Hp = 100;
        characterData.Attack = 10;
        characterData.Speed = 5.0f;
        characterData.ActionValue = 0.0f;
        
        Debug.Log("PlayerController has rasing "  );
    }
}

