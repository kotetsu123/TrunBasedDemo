using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveSystemDebugTester : MonoBehaviour
{
    [ContextMenu("Save Game")]
    private void SaveGame()
    {
        SaveSystem.Save();
    }
}
