using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorHelper
{
    /// <summary>
    /// 创建资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetName"></param>
    /// <param name="overWrite"></param>
    /// <param name="focusOnAsset"></param>
    /// <returns></returns>
    public static T CreateAsset<T>(string name, bool overWrite, bool focusOnAsset) where T : ScriptableObject
    {
        // 获取路径
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
            path = "Assets";
        else if (Path.GetExtension(path) != "")
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

        return CreateAsset<T>(name, path, overWrite, focusOnAsset);
    }

    public static T CreateAsset<T>(string name, string path, bool overWrite, bool focusOnAsset) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        // 资源名
        if (string.IsNullOrEmpty(name))
            name = string.Format("New {0}", typeof(T).ToString());
        string assetPathAndName = path + "/" + name + ".asset";

        // 覆盖同名资源
        if (!overWrite)
            assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(assetPathAndName);

        // 创建资源
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();

        if (focusOnAsset)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
        return asset;
    }
}
