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
    /// 地图存储路径
    /// </summary>
    private static string s_EditorMapSavePath = AssetPathDefine.developDataPath + "/map";
    public static string mapSavePath
    {
        get
        {
            if (string.IsNullOrEmpty(s_EditorMapSavePath))
            {
                var dataPath = Application.dataPath;
                dataPath = dataPath.Substring(0, dataPath.LastIndexOf("/"));
                s_EditorMapSavePath = System.IO.Path.Combine(dataPath + "/data/", AssetPathDefine.dataFolderName.ToLower() + "/map");
            }
            return s_EditorMapSavePath;
        }
    }

    /// <summary>
    /// 地图存储路径
    /// </summary>
    private static readonly string kExternalMapSavePath = AssetPathDefine.externalDataPath + "/map";
    public static string externalMapSavePath { get { return kExternalMapSavePath; } }


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
