using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVTable
{
    private Dictionary<byte[], CSVBytesData> m_CSVBytesDataDict = new Dictionary<byte[], CSVBytesData>(new ByteArrayComparer());
    private List<TableField> m_FieldList = new List<TableField>();

    public void Init(Dictionary<byte[], CSVBytesData> csvBytesDataDict, List<TableField> fieldList)
    {
        m_CSVBytesDataDict = csvBytesDataDict;
        m_FieldList = fieldList;
    }

    public virtual void Unload(bool isRemove = true)
    {
        m_CSVBytesDataDict.Clear();
        m_FieldList.Clear();
    }

    public CSVBytesData GetByKey(int key1)
    {
        byte[] key = CSVHelper.GetKey(key1);
        CSVBytesData csvBytesData = GetCSVBytesData(key);
        return csvBytesData;
    }

    public CSVBytesData GetByKey(int key1, int key2)
    {
        byte[] key = CSVHelper.GetKey(key1, key2);
        CSVBytesData csvBytesData = GetCSVBytesData(key);
        return csvBytesData;
    }

    public CSVBytesData GetByKey(int key1, int key2, int key3)
    {
        byte[] key = CSVHelper.GetKey(key1, key2, key3);
        CSVBytesData csvBytesData = GetCSVBytesData(key);
        return csvBytesData;
    }

    public CSVBytesData GetByKey(int key1, int key2, int key3, int key4)
    {
        byte[] key = CSVHelper.GetKey(key1, key2, key3, key4);
        CSVBytesData csvBytesData = GetCSVBytesData(key);
        return csvBytesData;
    }

    public CSVBytesData GetByKey(int key1, int key2, int key3, int key4, int key5)
    {
        byte[] key = CSVHelper.GetKey(key1, key2, key3, key4, key5);
        CSVBytesData csvBytesData = GetCSVBytesData(key);
        return csvBytesData;
    }

    public CSVBytesData GetCSVBytesData(byte[] key)
    {
        CSVBytesData csvBytesData;
        if (m_CSVBytesDataDict.TryGetValue(key, out csvBytesData))
            return csvBytesData;
        else
            return null;
    }

    public Dictionary<byte[], CSVBytesData> GetAllCSVBytesData()
    {
        return m_CSVBytesDataDict;
    }
}
