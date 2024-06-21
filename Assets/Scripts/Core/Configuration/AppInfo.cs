using LitJson;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AppInfo
{
    private static readonly string s_AppVersion = "AppVersion";
    private static readonly string s_ResVersion = "ResVersion";

    private static string filePath { get { return System.IO.Path.Combine(AssetPathDefine.webBasePath, "record.txt"); } }

    /// <summary>
    /// 游戏版本号
    /// </summary>
    public static int appVersion { private set; get; }

    /// <summary>
    /// 游戏版本号
    /// </summary>
    public static int gameVersion { private set; get; }

    /// <summary>
    /// 资源版本号
    /// </summary>
    public static int resVersion { private set; get; }

    public static void Setup(int version)
    {
        gameVersion = version;
    }

    public static void Init()
    {
        string text = Utils.ReadAllText(filePath);
        if (!string.IsNullOrEmpty(text))
        {
            JsonData jData = JsonMapper.ToObject(text);
            appVersion = GetIntDataByJson(s_AppVersion, jData);
            resVersion = GetIntDataByJson(s_ResVersion, jData);
        }
        else
        {
            appVersion = 0;
            resVersion = 0;
        }
    }

    public static void SaveAll(int gameVersion, int resVersion)
    {
        appVersion = gameVersion;
        AppInfo.resVersion = resVersion;
        JsonData jData = new JsonData();
        jData[s_AppVersion] = appVersion.ToString();
        jData[s_ResVersion] = resVersion.ToString();
        Utils.WriteAllText(filePath, jData.ToJson());
    }


    private static int GetIntDataByJson(string property, JsonData jData)
    {
        int data = 0;
        if (jData != null)
        {
            if (jData.Keys.Contains(property))
            {
                string str = jData[property].ToString();
                int.TryParse(str, out data);
            }
        }
        return data;
    }
}
