using LZ4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CompressionUtils
{
    /// <summary>
    /// 压缩文件(支持lz4)
    /// </summary>
    /// <param name="srcFile"></param>
    /// <param name="distFile"></param>
    /// <param name="processDelegate"></param>
    public static void CompressFile(string srcFile, string distFile, System.Action<float> processDelegate = null)
    {
        Stream outStream = null;
        FileStream ms = new FileStream(distFile, FileMode.Create);

        // 根据后续进行压缩
        string suffix = "";
        int pos = distFile.LastIndexOf(".");
        if (pos != -1) suffix = distFile.Substring(pos);
        if (suffix == ".lz4")
        {
            outStream = new LZ4Stream(ms, LZ4StreamMode.Compress);
        }
        else
        {
            ms.Close();
            return;
        }

        FileStream intStream = new FileStream(srcFile, FileMode.Open);
        ConvertStream(intStream, outStream, processDelegate);
        intStream.Close();
        outStream.Flush();
        outStream.Close();
    }

    public static void ConvertStream(Stream inStream, Stream outStream, Action<float> processDelegate = null)
    {
        byte[] bytes = new byte[4096];
        int count = 0;

        long fileLength = inStream.Length;
        long processLength = 0;
        while ((count = inStream.Read(bytes, 0, 4096)) != 0)
        {
            processLength += count;
            outStream.Write(bytes, 0, count);
            if (processDelegate != null)
            {
                processDelegate((float)processLength / fileLength);
            }
        }
    }
}
