using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// Resources路径转换成AssetPath
    /// </summary>
    /// <param name="resourcesPath"></param>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public static string ToAssetPath(string resourcesPath, string suffix)
    {
        return string.Format("{0}{1}.{2}", AssetPathDefine.resFolder, resourcesPath, suffix);
    }

    #region Time

    /// <summary>
    /// 系统当前时间（毫秒）
    /// 1毫秒=1000微秒 1微秒=1000毫微秒（纳秒）Ticks是以100纳秒为间隔的间隔数
    /// </summary>
    public static long currentTimeMillis
    {
        get { return (long)(Time.time * 1000); }
    }

    #endregion
}
