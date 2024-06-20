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
        StringWriter sw = new StringWriter();
        using (JsonWriter jsonWriter = new JsonTextWriter(sw))
        {
            jsonWriter.Formatting = Formatting.Indented;
            JsonMapper.ToJson(obj, jsonWriter);
        }
        return sw.ToString();
    }
}
