using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVManager : Singleton<CSVManager>
{
    private readonly Dictionary<string, CSVData> m_DataDict = new Dictionary<string, CSVData>();

    private readonly Dictionary<string, CSVTable> m_TableDict = new Dictionary<string, CSVTable>();

    public CSVTable GetCSVTable(string tableName)
    {
        string path = CSVHelper.Combine(tableName + ".bytes");

        CSVTable csvTable;
        if (!m_TableDict.ContainsKey(tableName))
        {
            csvTable = new CSVTable();

            Dictionary<byte[], CSVBytesData> csvBytesDataDict = new Dictionary<byte[], CSVBytesData>(new ByteArrayComparer());
#if UNITY_EDITOR
            byte[] data = CSVHelper.GetBytesFromFile(path);
#else
            byte[] data = CSVHelper.GetBytes(path);
#endif
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = null;
            List<TableField> tableFieldList = new List<TableField>();
            try
            {
                reader = new BinaryReader(stream);

                // 行数，不包括标题行
                int rowsCount = reader.ReadInt32();

                // 列数（也就是csv表每行字段数）
                int columnsCount = reader.ReadInt32();

                for (int i = 0; i < columnsCount; i++)
                {
                    TableField tableField = new TableField();
                    tableField.fieldType = (TableBaseType)reader.ReadByte();
                    tableFieldList.Add(tableField);
                }

                for (int i = 0; i < rowsCount; i++)
                {
                    int keyBytesCount = reader.ReadInt32();
                    byte[] keyBytes = reader.ReadBytes(keyBytesCount);

                    int count = reader.ReadInt32();
                    byte[] allFieldData = reader.ReadBytes(count);
                    CSVBytesData csvDate = new CSVBytesData();
                    csvDate.Init(allFieldData, tableFieldList, tableName);
                    csvBytesDataDict.Add(keyBytes, csvDate);
                }
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("{0}表 二进制数据解析出错 {1}", tableName, exception.StackTrace);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            csvTable.Init(csvBytesDataDict, tableFieldList);
            m_TableDict.Add(tableName, csvTable);
        }
        else
        {
            csvTable = m_TableDict[tableName];
        }

        return csvTable;
    }

    public void AddCSVData(string tableName, CSVData csvData)
    {
        m_DataDict.Add(tableName, csvData);
    }

    public void RemoveCSVData(string tableName)
    {
        if (m_DataDict.ContainsKey(tableName))
            m_DataDict.Remove(tableName);

        if (m_TableDict.ContainsKey(tableName))
            m_TableDict.Remove(tableName);
    }

    public void UnloadData(string tableName)
    {
        if (m_DataDict.ContainsKey(tableName))
            m_DataDict[tableName].UnloadData();
        if (m_TableDict.ContainsKey(tableName))
            m_TableDict[tableName].Unload();

        RemoveCSVData(tableName);
    }

    public void UnloadAllTable()
    {
        var iter = m_DataDict.GetEnumerator();
        while (iter.MoveNext())
        {
            iter.Current.Value.UnloadData(false);
        }
        iter.Dispose();

        m_DataDict.Clear();
        m_TableDict.Clear();
    }
}
