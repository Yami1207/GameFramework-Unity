using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoroutineRunner
{
    /// <summary>
    /// 全局启动协程
    /// </summary>
    /// <param name="function"></param>
    public static Coroutine Run(IEnumerator function)
    {
        if (Application.isPlaying)
        {
            return Globals.StartCoroutine(function);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 阻塞式调用
    /// </summary>
    /// <param name="function"></param>
    public static void Wait(IEnumerator function)
    {
        while (function.MoveNext())
        {
            if (function.Current != null)
            {
                var itor = function.Current as IEnumerator;
                if (itor != null)
                    Wait(itor);
                else
                    return;
            }
        }
    }
}
