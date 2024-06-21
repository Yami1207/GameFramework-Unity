using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AssetPathDefine
{
    public static readonly string dataFolderName = "Data";
    public static readonly string dataZipName = "Data.zip";

    /// <summary>
    /// 项目中存放资源的目录
    /// </summary>
    public static string resFolder { get { return "Assets/Res/"; } }

    /// <summary>
    /// 资源存放的基本目录（持久化目录）
    /// </summary>
    public static string webBasePath
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Application.dataPath + "/../HotUpdate/";
#else
            return Application.persistentDataPath;
#endif
        }
    }

    /// <summary>
    /// 存放下载资源的目录
    /// </summary>
    private static string s_ExternalFilePath = string.Empty;
    public static string externalFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(s_ExternalFilePath))
                s_ExternalFilePath = System.IO.Path.Combine(webBasePath, "http_res");
            return s_ExternalFilePath;
        }
    }

    /// <summary>
    /// 存放数据文件的目录
    /// </summary>
    private static string s_ExternalDataPath = string.Empty;
    public static string externalDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(s_ExternalDataPath))
                s_ExternalDataPath = System.IO.Path.Combine(externalFilePath, dataFolderName.ToLower());
            return s_ExternalDataPath;
        }
    }

    /// <summary>
    /// 存放数据文件的目录 - 开发模式
    /// </summary>
    private static string s_DevelopDataPath = string.Empty;
    public static string developDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(s_DevelopDataPath))
                s_DevelopDataPath = System.IO.Path.Combine(Application.dataPath + "/../data/", dataFolderName.ToLower());
            return s_DevelopDataPath;
        }
    }

    /// <summary>
    /// 项目中数据文件路径
    /// </summary>
    private static string s_ProjectDataPath = string.Empty;
    public static string projectDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(s_ProjectDataPath))
                s_ProjectDataPath = resFolder + dataFolderName;
            return s_ProjectDataPath;
        }
    }

    /// <summary>
    /// 数据zip目录
    /// </summary>
    public static string dataFloder
    {
        get { return Application.streamingAssetsPath + "/" + dataFolderName; }
    }

    /// <summary>
    /// 存放bundle的文件夹名
    /// </summary>
    public static string assetBundleFolder
    {
        get { return "AssetBundles"; }
    }

    /// <summary>
    /// bundle表命名
    /// </summary>
    public static string bundleTableFileName
    {
        get { return "BundleTable.json"; }
    }

    /// <summary>
    /// 依赖bundle表
    /// </summary>
    public static string depBundleTableFileName
    {
        get { return "DepBundleTable.json"; }
    }

    /// <summary>
    /// 常驻内存列表
    /// </summary>
    public static string residentBundleTableName
    {
        get { return "ResidentBundles.json"; }
    }

    /// <summary>
    /// 指定的Bundle外部路径
    /// </summary>
    private static string s_ExternalBundlePath = string.Empty;
    public static string externalBundlePath
    {
        get
        {
            if (string.IsNullOrEmpty(s_ExternalBundlePath))
                s_ExternalBundlePath = System.IO.Path.Combine(externalFilePath, assetBundleFolder);
            return s_ExternalBundlePath;
        }
    }

    /// <summary>
    /// 打包后的数据文件路径(非热更数据压缩包)
    /// </summary>
    private static string s_PackedDataPath = string.Empty;
    public static string packedDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(s_PackedDataPath))
                s_PackedDataPath = dataFloder + "/" + dataZipName;
            return s_PackedDataPath;
        }
    }
}
