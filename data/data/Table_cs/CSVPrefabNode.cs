using System.Collections.Generic;
using System;

public class CSVPrefabNode : CSVData
{
    private static readonly string s_TableName = "PrefabNode";

    private static readonly CSVPrefabNode s_instance = new CSVPrefabNode();
    public static CSVPrefabNode instance { get {return s_instance; } }

    private static Dictionary<byte[], CSVPrefabNode> s_DataDict = new Dictionary<byte[], CSVPrefabNode>();

    #region 定义字段
    
    /// <summary>
    /// id
    /// </summary>
    public int id { private set; get; }

    /// <summary>
    /// 
    /// </summary>
    public int asset_id { private set; get; }

    /// <summary>
    /// 
    /// </summary>
    public bool instancing { private set; get; }

    /// <summary>
    /// 可视距离
    /// </summary>
    public int visible { private set; get; }

    #endregion

    #region Override

    protected override string Name()
    {
        return s_TableName;
    }

    public override void UnloadData(bool isRemove = true)
    {
        base.UnloadData(isRemove);
        s_DataDict.Clear();

        // 清除缓存
        if (isRemove)
            CSVManager.instance.RemoveCSVData(Name());
    }

    #endregion

    #region 功能函数

    /// <summary>
    /// 通过key获取对象
    /// </summary>
    private CSVPrefabNode Get(byte[] key, bool isCache = true)
    {
        LoadCSVTable();

        CSVPrefabNode csvData;
        if (!s_DataDict.TryGetValue(key, out csvData))
        {
            CSVBytesData bytesData = GetCSVBytesData(key);
            if (bytesData == null)
                return null;
            csvData = GetCSVData(bytesData);
            if (isCache)
            {
                if (!s_DataDict.ContainsKey(key))
                    s_DataDict.Add(key, csvData);
            }
        }
        return csvData;
    }

    private CSVPrefabNode GetCSVData(CSVBytesData bytesData)
    {
        CSVPrefabNode csvData = null;
        try
        {
            csvData = new CSVPrefabNode();
            csvData.bytesData = bytesData;
            bytesData.BeginLoad();

            // 读取字段
            csvData.id = bytesData.ReadToInt32();
            csvData.asset_id = bytesData.ReadToInt32();
            csvData.instancing = bytesData.ReadToBoolean();
            csvData.visible = bytesData.ReadToInt32();

        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat("{0}表 解析出错 {1}", s_TableName, exception.StackTrace);
            return null;
        }

        return csvData;
    }

    private Dictionary<byte[], CSVPrefabNode> GetAll(bool isCache)
    {
        LoadCSVTable();

        Dictionary<byte[], CSVPrefabNode> allDict = new Dictionary<byte[], CSVPrefabNode>();
        if (s_DataDict.Count == GetAllCSVBytesData().Count)
        {
            allDict = s_DataDict;
        }
        else
        {
            s_DataDict.Clear();
            var iter = GetAllCSVBytesData().GetEnumerator();
            while (iter.MoveNext())
            {
                CSVPrefabNode csvData = Get(iter.Current.Key);
                allDict.Add(iter.Current.Key, csvData);
            }
            iter.Dispose();

            if (isCache)
                s_DataDict = allDict;
        }
        return allDict;
    }

    #endregion

    #region 静态函数

    public static Dictionary<byte[], CSVPrefabNode> GetAllDict(bool isCache = false)
    {
        return instance.GetAll(isCache);
    }

    public static void Load()
    {
        instance.LoadCSVTable();
    }

    public static void Unload(bool isRemove = true)
    {
        instance.UnloadData(isRemove);
    }

    #endregion
}
