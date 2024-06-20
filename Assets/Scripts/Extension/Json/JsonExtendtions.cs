using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonExtendtions
{
    public static void SetValue(this JToken token, string path, bool errorWhenNoMatch, bool value)
    {
        if (token == null) return;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return;
        select.Replace(value);
    }

    public static bool GetValue(this JToken token, string path, bool errorWhenNoMatch, bool defaultValue)
    {
        if (token == null) return defaultValue;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return defaultValue;
        return (bool)select;
    }

    public static int GetValue(this JToken token, string path, bool errorWhenNoMatch, int defaultValue)
    {
        if (token == null) return defaultValue;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return defaultValue;
        return (int)select;
    }

    public static float GetValue(this JToken token, string path, bool errorWhenNoMatch, float defaultValue)
    {
        if (token == null) return defaultValue;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return defaultValue;
        return (float)select;
    }

    public static double GetValue(this JToken token, string path, bool errorWhenNoMatch, double defaultValue)
    {
        if (token == null) return defaultValue;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return defaultValue;
        return (double)select;
    }

    public static void SetValue(this JToken token, string path, bool errorWhenNoMatch, string value = "")
    {
        if (token == null) return;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return;
        select.Replace(value);
    }

    public static string GetValue(this JToken token, string path, bool errorWhenNoMatch, string defaultValue = "")
    {
        if (token == null) return defaultValue;
        JToken select = token.SelectToken(path, errorWhenNoMatch);
        if (select == null) return defaultValue;
        return select.ToString();
    }
}
