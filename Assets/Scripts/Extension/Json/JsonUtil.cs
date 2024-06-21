using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class JsonUtil
{
    /// <summary>
    /// 输出格式好看的json
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToPrettyJson(object obj)
    {
        StringBuilder sb = new StringBuilder();
        LitJson.JsonWriter jsonWriter = new LitJson.JsonWriter(sb);
        jsonWriter.PrettyPrint = true;
        LitJson.JsonMapper.ToJson(obj, jsonWriter);
        return sb.ToString();
    }
}
