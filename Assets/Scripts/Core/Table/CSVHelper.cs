using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UIElements;

public static class CSVHelper
{
    private static readonly string s_PathName = "data/table";

    public static string Combine(string csv_name)
    {
        return Path.Combine(s_PathName, csv_name);
    }

    /// <summary>
    /// 获取实际数据文件路径
    /// </summary>
    /// <param name="dataPath"></param>
    /// <returns></returns>
    public static string GetActualDataPath(string dataPath)
    {
#if UNITY_EDITOR
        return AssetPathDefine.developDataPath + dataPath.Substring(AssetPathDefine.dataFolderName.Length);
#else
        return AssetPathDefine.externalDataPath + dataPath.Substring(AssetPathDefine.dataFolderName.Length);
#endif
    }

    public static byte[] GetBytes(string tablePath, bool onlyResouces = false)
    {
        return null;
    }

    public static byte[] GetBytesFromFile(string tablePath)
    {
        byte[] bytes = null;

        string filePath = tablePath;
        if (IsDataPath(tablePath))
            filePath = GetActualDataPath(filePath);

        if (File.Exists(filePath))
        {
            var fileStream = File.OpenRead(filePath);
            BinaryReader reader = new BinaryReader(fileStream);
            bytes = new byte[fileStream.Length];
            reader.Read(bytes, 0, bytes.Length);
            fileStream.Close();
        }

        return bytes;
    }

    /// <summary>
    /// 是否为数据路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool IsDataPath(string path)
    {
        return path.StartsWith(AssetPathDefine.dataFolderName, StringComparison.OrdinalIgnoreCase);
    }

    #region 获取唯一键值

    public static byte[] GetKey(int key1)
    {
        return BitConverter.GetBytes(key1);
    }

    public static byte[] GetKey(int key1, int key2)
    {
        byte[] bytes = new byte[8];
        byte[] bytes1 = BitConverter.GetBytes(key1);
        byte[] bytes2 = BitConverter.GetBytes(key2);
        for (int i = 0; i < bytes.Length; ++i)
        {
            if (i >= 0 && i < 4)
                bytes[i] = bytes1[i];
            else
                bytes[i] = bytes2[i - 4];
        }
        return bytes;
    }

    public static byte[] GetKey(int key1, int key2, int key3)
    {
        byte[] bytes = new byte[12];
        byte[] bytes1 = BitConverter.GetBytes(key1);
        byte[] bytes2 = BitConverter.GetBytes(key2);
        byte[] bytes3 = BitConverter.GetBytes(key3);
        for (int i = 0; i < bytes.Length; ++i)
        {
            if (i >= 0 && i < 4)
                bytes[i] = bytes1[i];
            else if (i >= 4 && i < 8)
                bytes[i] = bytes2[i - 4];
            else
                bytes[i] = bytes3[i - 8];
        }
        return bytes;
    }

    public static byte[] GetKey(int key1, int key2, int key3, int key4)
    {
        byte[] bytes = new byte[16];
        byte[] bytes1 = BitConverter.GetBytes(key1);
        byte[] bytes2 = BitConverter.GetBytes(key2);
        byte[] bytes3 = BitConverter.GetBytes(key3);
        byte[] bytes4 = BitConverter.GetBytes(key4);
        for (int i = 0; i < bytes.Length; ++i)
        {
            if (i >= 0 && i < 4)
                bytes[i] = bytes1[i];
            else if (i >= 4 && i < 8)
                bytes[i] = bytes2[i - 4];
            else if (i >= 8 && i < 12)
                bytes[i] = bytes3[i - 8];
            else
                bytes[i] = bytes4[i - 12];
        }
        return bytes;
    }

    public static byte[] GetKey(int key1, int key2, int key3, int key4, int key5)
    {
        byte[] bytes = new byte[20];
        byte[] bytes1 = BitConverter.GetBytes(key1);
        byte[] bytes2 = BitConverter.GetBytes(key2);
        byte[] bytes3 = BitConverter.GetBytes(key3);
        byte[] bytes4 = BitConverter.GetBytes(key4);
        byte[] bytes5 = BitConverter.GetBytes(key5);
        for (int i = 0; i < bytes.Length; ++i)
        {
            if (i >= 0 && i < 4)
                bytes[i] = bytes1[i];
            else if (i >= 4 && i < 8)
                bytes[i] = bytes2[i - 4];
            else if (i >= 8 && i < 12)
                bytes[i] = bytes3[i - 8];
            else if (i >= 12 && i < 16)
                bytes[i] = bytes4[i - 12];
            else
                bytes[i] = bytes5[i - 16];
        }
        return bytes;
    }

    #endregion
}
