using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoroutineRunner
{
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
