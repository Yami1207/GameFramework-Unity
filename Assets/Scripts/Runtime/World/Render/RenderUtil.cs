using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderUtil
{
    private static int s_MainThreadId;

    public static bool isMainThread {  get { return s_MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId; } }

    static RenderUtil()
    {
        s_MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
    }

    public static long RealtimeSinceStartupLong()
    {
        return (long)(1000 * Time.realtimeSinceStartupAsDouble);
    }

    public static long CreateMSFromRealTime(long offset)
    {
        long realtime = RealtimeSinceStartupLong();
        realtime += offset;
        return realtime;
    }

    public static bool IsRealTimeOut(long time)
    {
        return RealtimeSinceStartupLong() > time;
    }
}
