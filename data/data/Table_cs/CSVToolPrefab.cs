using System.Collections.Generic;
using System;

public class CSVToolPrefab : CSVData
{
    private static readonly string s_TableName = "ToolPrefab";

    private static readonly CSVToolPrefab s_instance = new CSVToolPrefab();
    public static CSVToolPrefab instance { get {return s_instance; } }

    private static Dictionary<byte[], CSVToolPrefab> s_DataDict = new Dictionary<byte[], CSVToolPrefab>();

    #region 定义字段
    
    /// <summary>
    /// id
    /// </summary>
    public int id { private set; get; }

    /// <summary>
    /// path
    /// </summary>
    public string path { private set; get; }

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
    private CSVToolPrefab Get(byte[] key, bool isCache = true)
    {
        LoadCSVTable();

        CSVToolPrefab csvData;
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

    private CSVToolPrefab GetCSVData(CSVBytesData bytesData)
    {
        CSVToolPrefab csvData = null;
        try
        {
            csvData = new CSVToolPrefab();
            csvData.bytesData = bytesData;
            bytesData.BeginLoad();

            // 读取字段
            csvData.id = bytesData.ReadToInt32();
            csvData.path = bytesData.ReadString();

        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat("{0}表 解析出错 {1}", s_TableName, exception.StackTrace);
            return null;
        }

        return csvData;
    }

    private Dictionary<byte[], CSVToolPrefab> GetAll(bool isCache)
    {
        LoadCSVTable();

        Dictionary<byte[], CSVToolPrefab> allDict = new Dictionary<byte[], CSVToolPrefab>();
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
                CSVToolPrefab csvData = Get(iter.Current.Key);
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

    public static Dictionary<byte[], CSVToolPrefab> GetAllDict(bool isCache = false)
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
