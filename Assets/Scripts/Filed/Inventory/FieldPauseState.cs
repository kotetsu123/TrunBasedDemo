using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FieldPauseState 
{
    public static bool IsPaused { get; private set; }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Debug.Log($"[FieldPauseState] isPaused={IsPaused}");
    }
    public static void Clear()
    {
        IsPaused = false;
    }

}
