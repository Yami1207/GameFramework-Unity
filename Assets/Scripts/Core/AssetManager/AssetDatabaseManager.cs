#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

public class AssetDatabaseManager : IAssetLoader
{
    public void Init()
    {
    }

    public T LoadAsset<T>(int assetId) where T : Object
    {
        var info = AssetInfo.GetAssetInfo(assetId);
        if (info == null)
            return null;
        return AssetDatabase.LoadAssetAtPath<T>(info.assetPath);
    }

    public T LoadAsset<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}

#endif
