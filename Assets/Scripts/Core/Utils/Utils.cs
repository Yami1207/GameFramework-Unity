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
}
