using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AssetPathDefine
{
    public static readonly string dataFolderName = "Data";

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
    private static string m_ExternalFilePath = string.Empty;
    public static string externalFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(m_ExternalFilePath))
                m_ExternalFilePath = System.IO.Path.Combine(webBasePath, "http_res");
            return m_ExternalFilePath;
        }
    }

    /// <summary>
    /// 存放数据文件的目录
    /// </summary>
    private static string m_ExternalDataPath = string.Empty;
    public static string externalDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(m_ExternalDataPath))
                m_ExternalDataPath = System.IO.Path.Combine(externalFilePath, dataFolderName.ToLower());
            return m_ExternalDataPath;
        }
    }

    /// <summary>
    /// 存放数据文件的目录 - 开发模式
    /// </summary>
    private static string m_DevelopDataPath = string.Empty;
    public static string developDataPath
    {
        get
        {
            if (string.IsNullOrEmpty(m_DevelopDataPath))
                m_DevelopDataPath = System.IO.Path.Combine(Application.dataPath + "/../data/", dataFolderName.ToLower());
            return m_DevelopDataPath;
        }
    }

    /// <summary>
    /// 数据zip目录
    /// </summary>
    public static string dataFloder
    {
        get { return Application.streamingAssetsPath + "/" + dataFolderName; }
    }
}
