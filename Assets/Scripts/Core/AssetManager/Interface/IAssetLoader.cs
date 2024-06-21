using UnityEngine;

public interface IAssetLoader
{
    /// <summary>
    /// 加载资源接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetId"></param>
    /// <returns></returns>
    T LoadAsset<T>(int assetId) where T : Object;
}
