using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// Resources路径转换成AssetPath
    /// </summary>
    /// <param name="resourcesPath"></param>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public static string ToAssetPath(string resourcesPath, string suffix)
    {
        return string.Format("{0}{1}.{2}", AssetPathDefine.resFolder, resourcesPath, suffix);
    }

    #region String

    /// <summary>
    /// 获取去除BOM的字符串
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static string GetStringWithoutBOM(byte[] array)
    {
        if (array.Length > 3)
        {
            //去除bom
            if (array[0] == 0xef && array[1] == 0xbb && array[2] == 0xbf)
                return System.Text.Encoding.UTF8.GetString(array, 3, array.Length - 3);
        }

        return System.Text.Encoding.UTF8.GetString(array, 0, array.Length);
    }

    /// <summary>
    /// 获取文件前缀,例如 table/hero.csv -> table/hero
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string GetPrefix(string filename, string c)
    {
        int index = filename.LastIndexOf(c);
        if (index > 0)
            filename = filename.Substring(0, index);
        return filename;
    }

    /// <summary>
    /// 获取文件后缀,例如hero.csv -> .csv
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string GetSuffix(string filename, string c)
    {
        int index = filename.LastIndexOf(c);
        if (index > 0)
            filename = filename.Substring(index);
        return filename;
    }

    #endregion

    #region File

    public static string ReadAllText(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("ReadAllText Exception {0}", ex);
            return null;
        }
    }

    public static bool WriteAllText(string path, string text)
    {
        bool isSucess = false;
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        try
        {
            File.WriteAllText(path, text, Encoding.UTF8);
            isSucess = true;
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("WriteAllText Exception {0}", ex);
            isSucess = false;
        }
        return isSucess;
    }

    #endregion

    #region Time

    /// <summary>
    /// 系统当前时间（毫秒）
    /// 1毫秒=1000微秒 1微秒=1000毫微秒（纳秒）Ticks是以100纳秒为间隔的间隔数
    /// </summary>
    public static long currentTimeMillis
    {
        get { return (long)(Time.time * 1000); }
    }

    #endregion

    #region Math

    public static bool TestPlanesAABB(ref Vector4[] planes, Vector3 minBounds, Vector3 maxBounds)
    {
        Vector3 min = Vector3.zero, max = Vector3.zero;
        for (int index = 0; index < 6; ++index)
        {
            Vector3 normal = new Vector3(planes[index].x, planes[index].y, planes[index].z);
            float planeDistance = planes[index].w;

            // X axis
            if (normal.x < 0)
            {
                min.x = minBounds.x;
                max.x = maxBounds.x;
            }
            else
            {
                min.x = maxBounds.x;
                max.x = minBounds.x;
            }

            // Y axis
            if (normal.y < 0)
            {
                min.y = minBounds.y;
                max.y = maxBounds.y;
            }
            else
            {
                min.y = maxBounds.y;
                max.y = minBounds.y;
            }

            // Z axis
            if (normal.z < 0)
            {
                min.z = minBounds.z;
                max.z = maxBounds.z;
            }
            else
            {
                min.z = maxBounds.z;
                max.z = minBounds.z;
            }

            var dot1 = normal.x * min.x + normal.y * min.y + normal.z * min.z;
            if (dot1 + planeDistance < 0)
                return false;
        }

        return true;
    }

    public static bool TestPlanesAABB(ref NativeArray<Vector4> planes, Vector3 minBounds, Vector3 maxBounds)
    {
        Vector3 min = Vector3.zero;
        for (int index = 0; index < 6; ++index)
        {
            Vector3 normal = new Vector3(planes[index].x, planes[index].y, planes[index].z);
            float planeDistance = planes[index].w;

            // X axis
            if (normal.x < 0)
            {
                min.x = minBounds.x;
            }
            else
            {
                min.x = maxBounds.x;
            }

            // Y axis
            if (normal.y < 0)
            {
                min.y = minBounds.y;
            }
            else
            {
                min.y = maxBounds.y;
            }

            // Z axis
            if (normal.z < 0)
            {
                min.z = minBounds.z;
            }
            else
            {
                min.z = maxBounds.z;
            }

            var dot1 = normal.x * min.x + normal.y * min.y + normal.z * min.z;
            if (dot1 + planeDistance < 0)
                return false;
        }

        return true;
    }

    #endregion
}
