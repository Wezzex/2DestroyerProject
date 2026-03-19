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
}
