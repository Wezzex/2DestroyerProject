using System.Runtime.CompilerServices;
using UnityEngine;

public static class Utility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInfo(object message)
    {
#if VERBOSE_LOGGING
        Debug.Log(message);
#endif
    }

    public static ShipController AIDebugTarget = null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogAI(object message, ShipController shipController)
    {
#if AI_LOGGING
        if (shipController == AIDebugTarget)
        {
            Debug.Log(message);
        }
#endif
    }
}
