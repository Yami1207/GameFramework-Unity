using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

/// <summary>
/// XML工具类
/// </summary>
public static class XMLTool
{
    public static string ToString(byte[] array)
    {
        if (array.Length > 3)
        {
            // 去除bom
            if (array[0] == 0xef && array[1] == 0xbb && array[2] == 0xbf)
                return System.Text.Encoding.UTF8.GetString(array, 3, array.Length - 3);
        }

        return System.Text.Encoding.UTF8.GetString(array, 0, array.Length);
    }

    public static string Attribute(SecurityElement node, string name)
    {
        return node.Attribute(name);
    }

    public static bool HasAttribute(SecurityElement node, string name)
    {
        return !(string.IsNullOrEmpty(node.Attribute(name)));
    }

    public static bool GetBoolAttribute(SecurityElement node, string name, bool defaultValue = false)
    {
        if (node == null || string.IsNullOrEmpty(name))
            return defaultValue;

        string result = Attribute(node, name);
        if (string.IsNullOrEmpty(result))
            return defaultValue;
        return ParseBool(result, defaultValue);
    }

    public static int GetIntAttribute(SecurityElement node, string name, int defaultValue = 0)
    {
        if (node == null || string.IsNullOrEmpty(name))
            return defaultValue;

        string result = Attribute(node, name);
        if (string.IsNullOrEmpty(result))
            return defaultValue;
        return ParseInt(result, defaultValue);
    }

    public static bool ParseBool(string text, bool defaultValue = false)
    {
        bool result = false;
        if (bool.TryParse(text, out result))
            return result;
        return defaultValue;
    }

    private static int ParseInt(string text, int defaultValue = 0)
    {
        int result = 0;
        if (int.TryParse(text, out result))
            return result;

        //  尝试解析浮点数
        float f = 0;
        if (float.TryParse(text, out f))
            return (int)(f * 100);
        return defaultValue;
    }
}
