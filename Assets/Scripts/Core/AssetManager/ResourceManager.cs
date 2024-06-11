using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ResourceManager : IAssetLoader
{
    public void Init()
    {

    }

    public T LoadAsset<T>(int assetId) where T : Object
    {
        var info = AssetInfo.GetAssetInfo(assetId);
        if (info == null)
            return null;
        return Resources.Load<T>(info.resourcesPath);
    }

    public T LoadAsset<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }
}
