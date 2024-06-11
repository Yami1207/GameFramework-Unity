using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CSVBytesData
{    
    private List<TableField> m_FieldList;

    private List<int> m_CursorIndex;

    private byte[] m_FieldData;

    private int m_Cursor;

    private string m_TableName;

    public void Init(byte[] allFieldData, List<TableField> fieldList, string tableName)
    {
        m_FieldData = allFieldData;
        m_FieldList = fieldList;
        m_TableName = tableName;
    }

    public void BeginLoad()
    {
        m_Cursor = 0;
    }

    public bool ReadToBoolean()
    {
        bool boolValue = BitConverter.ToBoolean(m_FieldData, m_Cursor);
        m_Cursor += 1;
        return boolValue;
    }

    public byte ReadToByte()
    {
        byte byteValue = m_FieldData[m_Cursor];
        ++m_Cursor;
        return byteValue;
    }

    public sbyte ReadToSByte()
    {
        sbyte sbyteValue;
        byte byteValue = m_FieldData[m_Cursor];
        if (byteValue >= 128)
            sbyteValue = (sbyte)(byteValue - 256);
        else
            sbyteValue = (sbyte)byteValue;
        ++m_Cursor;
        return sbyteValue;
    }

    public short ReadToInt16()
    {
        short shortValue = BitConverter.ToInt16(m_FieldData, m_Cursor);
        m_Cursor += 2;
        return shortValue;
    }

    public ushort ReadToUInt16()
    {
        ushort ushortValue = BitConverter.ToUInt16(m_FieldData, m_Cursor);
        m_Cursor += 2;
        return ushortValue;
    }

    public int ReadToInt32()
    {
        int intValue = BitConverter.ToInt32(m_FieldData, m_Cursor);
        m_Cursor += 4;
        return intValue;
    }

    public uint ReadToUInt32()
    {
        uint uintValue = BitConverter.ToUInt32(m_FieldData, m_Cursor);
        m_Cursor += 4;
        return uintValue;
    }

    public long ReadToInt64()
    {
        long longValue = BitConverter.ToInt64(m_FieldData, m_Cursor);
        m_Cursor += 8;
        return longValue;
    }

    public ulong ReadToUInt64()
    {
        ulong ulongValue = BitConverter.ToUInt64(m_FieldData, m_Cursor);
        m_Cursor += 8;
        return ulongValue;
    }

    public float ReadToSingle()
    {
        float floatValue = BitConverter.ToSingle(m_FieldData, m_Cursor);
        m_Cursor += 4;
        return floatValue;
    }

    public string ReadString()
    {
        int length = BitConverter.ToInt32(m_FieldData, m_Cursor);
        m_Cursor += 4;

        string strValue = Encoding.UTF8.GetString(m_FieldData, m_Cursor, length).Replace("\\n", "\n");
        m_Cursor += length;

        return strValue;
    }
}
