using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    private static Main s_MainScript;
    public static Main mainScript { get { return s_MainScript; } }

    public static void SetMainScript(Main main)
    {
        s_MainScript = main;
    }

    public static GameObject uiRoot { get { return s_MainScript != null ? s_MainScript.uiRoot : null; } }

    #region 全局协程

    /// <summary>
    /// 启动一个全局协程
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public static Coroutine StartCoroutine(IEnumerator function)
    {
        if (s_MainScript == null)
            return null;
        return s_MainScript.StartCoroutine(function);
    }

    /// <summary>
    /// 启动一个延时协程
    /// </summary>
    /// <param name="time"></param>
    /// <param name="after"></param>
    /// <returns></returns>
    public static IEnumerator StartDelayCoroutine(float time, System.Action after)
    {
        IEnumerator ienumerator = null;
        if (time <= 0)
            ienumerator = DelayCoroutine(after);
        else
            ienumerator = DelayCoroutine(time, after);
        StartCoroutine(ienumerator);
        return ienumerator;
    }

    private static IEnumerator DelayCoroutine(float time, System.Action after)
    {
        yield return new WaitForSeconds(time);
        if (after != null)
            after();
    }

    private static IEnumerator DelayCoroutine(System.Action after)
    {
        yield return null;
        if (after != null)
            after();
    }

    /// <summary>
    /// 关闭全局协程
    /// </summary>
    /// <param name="name"></param>
    public static void StopCoroutine(string name)
    {
        if (s_MainScript != null)
            s_MainScript.StopCoroutine(name);
    }

    /// <summary>
    /// 关闭全局协程
    /// </summary>
    /// <param name="function"></param>
    public static void StopCoroutine(IEnumerator function)
    {
        if (s_MainScript == null || function == null)
            return;
        s_MainScript.StopCoroutine(function);
    }

    #endregion
}
