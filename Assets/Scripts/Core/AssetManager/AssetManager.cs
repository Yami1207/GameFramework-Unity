using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TextCore;

public class AssetManager : Singleton<AssetManager>, IAssetLoader
{
    private Dictionary<int, CacheInfo> m_CacheAssetDict = new Dictionary<int, CacheInfo>();

    private readonly ResourceManager m_ResourceManager = new ResourceManager();

#if UNITY_EDITOR
    private readonly AssetDatabaseManager m_AssetDatabaseManager = new AssetDatabaseManager();
#endif

    private bool m_IsInit = false;
    public bool isInit { get { return m_IsInit; } }

    public void Init()
    {
        // 初始化Resources管理器
        m_ResourceManager.Init();

#if UNITY_EDITOR
        m_AssetDatabaseManager.Init();
#endif

        m_IsInit = true;
    }

    public GameObject LoadAssetAndInstantiate(int assetId)
    {
        GameObject prefab = LoadAsset<GameObject>(assetId);
        if (prefab == null)
            return null;
        return GameObject.Instantiate(prefab);
    }

    public T LoadAsset<T>(int assetId) where T : Object
    {
        CheckIsInitialized();

        if (assetId <= 0)
        {
            Debug.LogErrorFormat("资源ID无效(assetId = {0})", assetId);
            return null;
        }

        T asset = null;

        //从缓存获取
        asset = GetAssetFromCache(assetId) as T;

        if (asset == null)
            asset = m_ResourceManager.LoadAsset<T>(assetId);

#if UNITY_EDITOR
        if (asset == null)
            asset = m_AssetDatabaseManager.LoadAsset<T>(assetId);
#endif

        PutAssetToCache(assetId, asset);
        return asset;
    }

    public T LoadAsset<T>(string path) where T : Object
    {
        T asset = null;
#if UNITY_EDITOR
        asset = m_AssetDatabaseManager.LoadAsset<T>(AssetPathDefine.resFolder + path);
#else
        asset = m_ResourceManager.LoadAsset<T>(path);
#endif
        return asset;
    }

    /// <summary>
    /// 加载Resources资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : Object
    {
        return m_ResourceManager.LoadAsset<T>(path);
    }

    private void CheckIsInitialized()
    {
        if (AssetInfo.isInitialized == false)
        {
            //加载资源表
            AssetInfo.Load();
        }
    }

    #region 缓存

    private void PutAssetToCache(int assetId, Object asset)
    {
        if (asset == null)
            return;
        m_CacheAssetDict[assetId] = new CacheInfo(asset, assetId);
    }

    /// <summary>
    /// 加载资源 - 从缓存中获取
    /// </summary>
    /// <param name="assetId"></param>
    /// <returns></returns>
    private Object GetAssetFromCache(int assetId)
    {
        CacheInfo info = null;
        if (m_CacheAssetDict.TryGetValue(assetId, out info) == false)
            return null;

        info.Use();
        return info.asset;
    }

    #endregion
}
