using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSVData
{
    /// <summary>
    /// 表的所有行的二进制数据，注意只有是单例时，cvsTable才有值，否则是null
    /// </summary>
    public CSVTable csvTable;

    /// <summary>
    /// 表中某一行的二进制数据，注意只有是非单例时才有数据，否则是null
    /// </summary>
    public CSVBytesData bytesData;

    protected virtual string Name()
    {
        return "";
    }

    public void LoadCSVTable()
    {
        if (csvTable == null)
        {
            csvTable = CSVManager.instance.GetCSVTable(Name());
            CSVManager.instance.AddCSVData(Name(), this);
        }
    }

    public virtual void UnloadData(bool isRemove = true)
    {
        if (csvTable != null)
        {
            csvTable.Unload();
            csvTable = null;
        }
    }

    public CSVBytesData GetCSVBytesData(byte[] key)
    {
        return csvTable.GetCSVBytesData(key);
    }

    public Dictionary<byte[], CSVBytesData> GetAllCSVBytesData()
    {
        return csvTable.GetAllCSVBytesData();
    }
}
