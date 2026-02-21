using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targetable : MonoBehaviour
{
    public BaseController controller;
    private void Awake()
    {
        if(controller==null)
        controller = GetComponent<BaseController>();
    }
}
