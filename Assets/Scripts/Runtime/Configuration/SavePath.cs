using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static SavePath;

/// <summary>
/// 存档路径配置
/// </summary>
public static class SavePath
{
    public enum MapDirType
    {
        ShareMapSave
    }

    /// <summary>
    /// 地图存储路径(开发者模式)
    /// </summary>
    private static string s_EditorMapSavePath = AssetPathDefine.developDataPath + "/map";

    /// <summary>
    /// 地图存储路径
    /// </summary>
    private static readonly string s_ExternalMapSavePath = AssetPathDefine.externalDataPath + "/map";

    /// <summary>
    /// 地图存储路径
    /// </summary>
    public static string mapSavePath
    {
        get
        {
            return SettingManager.instance.developMode ? s_EditorMapSavePath : s_ExternalMapSavePath;
        }
    }

    /// <summary>
    /// 获取地图目录
    /// </summary>
    /// <param name="mapId"></param>
    /// <param name="type"></param>
    /// <param name="ensureExist"></param>
    /// <returns></returns>
    public static string GetMapDir(int mapID, MapDirType type, bool ensureExist = false)
    {
        string directory = "";
        switch (type)
        {
            case MapDirType.ShareMapSave:
                {
                    directory = GetShareMapPath(mapID);
                }
                break;
        }

        if (ensureExist && !string.IsNullOrEmpty(directory))
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        return directory;
    }

    private static string GetShareMapPath(int mapID)
    {
        return Path.Combine(mapSavePath, "Map_" + mapID);
    }
}
