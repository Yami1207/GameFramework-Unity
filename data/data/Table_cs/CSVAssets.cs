using System.Collections.Generic;
using System;

public class CSVAssets : CSVData
{
    private static readonly string s_TableName = "Assets";

    private static readonly CSVAssets s_instance = new CSVAssets();
    public static CSVAssets instance { get {return s_instance; } }

    private static Dictionary<byte[], CSVAssets> s_DataDict = new Dictionary<byte[], CSVAssets>();

    #region 定义字段
    
    /// <summary>
    /// id
    /// </summary>
    public int id { private set; get; }

    /// <summary>
    /// 路径
    /// </summary>
    public string dir { private set; get; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string name { private set; get; }

    /// <summary>
    /// 后缀名
    /// </summary>
    public string suffix { private set; get; }

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
    private CSVAssets Get(byte[] key, bool isCache = true)
    {
        LoadCSVTable();

        CSVAssets csvData;
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

    private CSVAssets GetCSVData(CSVBytesData bytesData)
    {
        CSVAssets csvData = null;
        try
        {
            csvData = new CSVAssets();
            csvData.bytesData = bytesData;
            bytesData.BeginLoad();

            // 读取字段
            csvData.id = bytesData.ReadToInt32();
            csvData.dir = bytesData.ReadString();
            csvData.name = bytesData.ReadString();
            csvData.suffix = bytesData.ReadString();

        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat("{0}表 解析出错 {1}", s_TableName, exception.StackTrace);
            return null;
        }

        return csvData;
    }

    private Dictionary<byte[], CSVAssets> GetAll(bool isCache)
    {
        LoadCSVTable();

        Dictionary<byte[], CSVAssets> allDict = new Dictionary<byte[], CSVAssets>();
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
                CSVAssets csvData = Get(iter.Current.Key);
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

    public static Dictionary<byte[], CSVAssets> GetAllDict(bool isCache = false)
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
