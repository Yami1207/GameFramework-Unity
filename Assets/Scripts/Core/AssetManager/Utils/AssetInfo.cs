using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;

public class AssetInfo
{
    /// <summary>
    /// 资源ID
    /// </summary>
    private int m_Identity;
    public int id { get { return m_Identity; } }

    /// <summary>
    /// 路径
    /// </summary>
    private string m_Directory;

    /// <summary>
    /// 资源名字
    /// </summary>
    private string m_AssetName;
    public string assetName { get { return m_AssetName; } }

    /// <summary>
    /// 文件目录
    /// </summary>
    private string m_FolderPath = null;
    private string folderPath
    {
        get
        {
            if (m_FolderPath == null)
                m_FolderPath = m_Directory.Replace('_', '/');
            return m_FolderPath;
        }
    }

    /// <summary>
    /// resources下目录
    /// </summary>
    private string m_ResourcesPath = null;
    public string resourcesPath
    {
        get
        {
            if (m_ResourcesPath == null)
                m_ResourcesPath = string.Format("{0}/{1}", m_Directory, m_AssetName);
            return m_ResourcesPath;
        }
    }

    /// <summary>
    /// 资源路径
    /// </summary>
    private string m_AssetPath = null;
    public string assetPath
    {
        get
        {
            if (m_AssetPath == null)
                m_AssetPath = Utils.ToAssetPath(resourcesPath, suffix);
            return m_AssetPath;
        }
    }

    /// <summary>
    /// 后缀名
    /// </summary>
    private string m_Suffix;
    public string suffix { get { return m_Suffix; } }

    public AssetInfo(int key, string dir, string name, string suffix)
    {
        m_Identity = key;
        m_Directory = dir;
        m_AssetName = name;
        m_Suffix = suffix;
    }

    /// <summary>
    /// 打印
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("id = {0}, assetPath = {1}", id, assetPath);
    }

    #region 静态方法

    private static Dictionary<int, AssetInfo> s_AssetInfoDict = new Dictionary<int, AssetInfo>();

    /// <summary>
    /// 获取枚举器
    /// </summary>
    /// <returns></returns>
    public static Dictionary<int, AssetInfo>.Enumerator GetEnumerator()
    {
        return s_AssetInfoDict.GetEnumerator();
    }

    public static bool isInitialized
    {
        get { return s_AssetInfoDict.Count > 0; }
    }

    /// <summary>
    /// 资源表获取委托
    /// </summary>
    public delegate List<AssetInfo> GetAssetTableHandler();
    public static GetAssetTableHandler getAssetTable;

    public static void Load(bool force = false)
    {
        if (force == false && isInitialized)
            return;

        if (getAssetTable == null)
        {
            Debug.LogErrorFormat("getAssetTable == null, 请先配置获取资源表的方法！");
            return;
        }

        List<AssetInfo> assetInfoList = getAssetTable();
        if (assetInfoList.Count == 0)
            return;

        // 清空旧数据
        Clear();

        var iter = assetInfoList.GetEnumerator();
        while (iter.MoveNext())
        {
            var data = iter.Current;
            int key = data.id;

#if UNITY_EDITOR
            if (s_AssetInfoDict.ContainsKey(key))
            {
                Debug.LogError("相同Key = " + key);
                continue;
            }
#endif
            s_AssetInfoDict.Add(key, data);
        }
    }

    public static void Clear()
    {
        s_AssetInfoDict.Clear();
    }

    public static AssetInfo GetAssetInfo(int assetId)
    {
#if UNITY_EDITOR
        if (s_AssetInfoDict.Count == 0)
        {
            Debug.LogError("资源表为空！请先初始化资源表！");
            return null;
        }
#endif

        AssetInfo info = null;
        s_AssetInfoDict.TryGetValue(assetId, out info);
        return info;
    }

    #endregion
}
