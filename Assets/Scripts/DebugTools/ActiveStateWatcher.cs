using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveStateWatcher : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogError($"[ActiveStateWatcher] {name} OnDisable called.",this);
    }
}

