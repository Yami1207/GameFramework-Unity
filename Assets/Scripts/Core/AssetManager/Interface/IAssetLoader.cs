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

    /// <summary>
    /// 加载资源接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    T LoadAsset<T>(string path) where T : Object;
}
