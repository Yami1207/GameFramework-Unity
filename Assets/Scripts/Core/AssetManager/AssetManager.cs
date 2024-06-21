using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetManager : Singleton<AssetManager>, IAssetLoader
{
    /// <summary>
    /// 资源加载回调
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="asset"></param>
    public delegate void ObjectCallback(UnityEngine.Object asset);

    /// <summary>
    /// 开启从AB加载资源(真机强制开启)
    /// </summary>
#if UNITY_EDITOR
    private bool m_EnableAssetBundleForEditor = false;
#endif
    public bool enableAssetBundle
    {
        get
        {
#if UNITY_EDITOR
            return m_EnableAssetBundleForEditor;
#else
            return true;
#endif
        }
    }

    private Dictionary<int, CacheInfo> m_CacheAssetDict = new Dictionary<int, CacheInfo>();

    private ResourceManager m_ResourceManager;

#if USING_ASSET_BUNDLE
    /// <summary>
    /// AssetBundle管理器
    /// </summary>
    private AssetBundleManager m_AssetBundleManager = null;
    public AssetBundleManager assetBundleManager { get { return m_AssetBundleManager; } }
#endif

#if UNITY_EDITOR
    private AssetDatabaseManager m_AssetDatabaseManager;
#endif

    /// <summary>
    /// 对象池管理器
    /// </summary>
    private PoolManager m_PoolManager = null;

    public void Init()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying)
            m_EnableAssetBundleForEditor = SettingManager.instance.enableAssetBundle;
        else
            m_EnableAssetBundleForEditor = false;
#endif

        if (enableAssetBundle)
        {
#if USING_ASSET_BUNDLE
            m_AssetBundleManager = new AssetBundleManager();
            m_AssetBundleManager.Init();
#endif
        }
        else
        {
#if UNITY_EDITOR
            m_AssetDatabaseManager = new AssetDatabaseManager();
            m_AssetDatabaseManager.Init();
#endif
        }

        // Resources管理器
        m_ResourceManager = new ResourceManager();
        m_ResourceManager.Init();

        // 对象池管理器
        m_PoolManager = new PoolManager();
    }

    public void DoStartScene()
    {
        m_PoolManager.Init();
    }

    public void DoExitScene()
    {
        m_CacheAssetDict.Clear();

        m_PoolManager.Destroy();

#if USING_ASSET_BUNDLE
        if (enableAssetBundle)
            m_AssetBundleManager.DoExitScene();
#endif

        m_ResourceManager.DoExitScene();
    }

    /// <summary>
    /// 卸载没有引用的资源
    /// </summary>
    public void UnloadUnusedAssets()
    {
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 加载并实例化GameObject
    /// </summary>
    /// <param name="assetId"></param>
    /// <returns></returns>
    public GameObject LoadAssetAndInstantiate(int assetId, bool isFromPool = true)
    {
        return LoadAssetAndInstantiate(assetId, Vector3.zero, Quaternion.identity, isFromPool);
    }

    /// <summary>
    /// 加载并实例化GameObject
    /// </summary>
    /// <param name="assetId"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="isFromPool"></param>
    /// <returns></returns>
    public GameObject LoadAssetAndInstantiate(int assetId, Vector3 position, Quaternion rotation, bool isFromPool = true)
    {
        CheckIsInitialized();

        // 从缓存管理器中获取
        if (isFromPool)
        {
            AssetInfo info = AssetInfo.GetAssetInfo(assetId);
            if (info == null)
                return null;

            GameObject go = m_PoolManager.Get(info.assetName);
            if (go != null)
            {
                go.transform.SetPositionAndRotation(position, rotation);
                return go;
            }
        }

        GameObject prefab = LoadAsset<GameObject>(assetId);
        if (prefab == null)
            return null;
        return Instantiate(prefab, position, rotation, false);
    }

    /// <summary>
    /// 实例化GameObject
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="isFromPool"></param>
    /// <returns></returns>
    public GameObject Instantiate(GameObject prefab, bool isFromPool = true)
    {
        return Instantiate(prefab, Vector3.zero, Quaternion.identity, isFromPool);
    }

    /// <summary>
    /// 实例化GameObject
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="isFromPool"></param>
    /// <returns></returns>
    public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, bool isFromPool = true)
    {
        if (prefab == null)
            return null;

        string name = prefab.name;
        GameObject go = null;

        // 从缓存管理器中获取
        if (isFromPool)
        {
            go = m_PoolManager.Get(name);
            if (go != null)
            {
                go.transform.SetPositionAndRotation(position, rotation);
                return go;
            }
        }

        // 实例化
        go = UnityEngine.Object.Instantiate(prefab, position, rotation) as GameObject;
        go.name = name;

        // 添加到预制表
        m_PoolManager.AddToPrefabMap(go, prefab);

        return go;
    }

    /// <summary>
    /// 回收GameObject，放回缓存池
    /// </summary>
    /// <param name="go"></param>
    public void RecycleGameObject(GameObject go)
    {
        m_PoolManager.ReturnToPool(go);
    }

    private void CheckIsInitialized()
    {
        if (AssetInfo.isInitialized == false)
        {
            //加载资源表
            AssetInfo.Load();
        }
    }

    #region 同步加载

    /// <summary>
    /// 加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="assetId"></param>
    /// <returns></returns>
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

#if USING_ASSET_BUNDLE
        // 从Assetbundle获取
        if (enableAssetBundle)
        {
            asset = m_AssetBundleManager.LoadAsset<T>(assetId);
        }
#endif

        // 从Resource获取
        if (asset == null)
            asset = m_ResourceManager.LoadAsset<T>(assetId);

#if UNITY_EDITOR
        if (asset == null)
            asset = m_AssetDatabaseManager.LoadAsset<T>(assetId);
#endif

        PutAssetToCache(assetId, asset);
        return asset;
    }

    /// <summary>
    /// 通过路径加载资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public Object LoadAsset(string path, string suffix = ".prefab")
    {
        return LoadAsset<Object>(path, suffix);
    }

    /// <summary>
    /// 加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadAsset<T>(string path, string suffix = ".prefab") where T : Object
    {
        T asset = null;

#if USING_ASSET_BUNDLE
        if (enableAssetBundle)
        {
            //从Assetbundle获取
            string assetPath = Utils.ToAssetPath(path, suffix);
            asset = m_AssetBundleManager.LoadAsset<T>(assetPath);
        }
#endif

#if UNITY_EDITOR
        if (asset == null && !enableAssetBundle)
        {
            string assetPath = Utils.ToAssetPath(path, suffix);
            asset = m_AssetDatabaseManager.LoadAsset<T>(assetPath);
        }
#endif

        // 从resources获取
        if (asset == null)
            asset = m_ResourceManager.LoadAsset<T>(path);

        return asset;
    }

    public byte[] LoadFileData(string originName)
    {
        if (string.IsNullOrEmpty(originName))
            return null;

        byte[] bytes = null;
        string filePath = originName;

        // 开发模式读Doc
        if (SettingManager.instance.developMode)
        {
            if (originName.StartsWith(AssetPathDefine.dataFolderName + "/") || originName.StartsWith(AssetPathDefine.dataFolderName.ToLower() + "/"))
                filePath = AssetPathDefine.developDataPath + originName.Substring(4);
            if (FilePath.Exists(filePath))
                bytes = File.ReadAllBytes(filePath);
        }
        else
        {
            if (originName.StartsWith(AssetPathDefine.dataFolderName + "/") || originName.StartsWith(AssetPathDefine.dataFolderName.ToLower() + "/"))
                filePath = AssetPathDefine.externalDataPath + originName.Substring(4);
            if (FilePath.Exists(filePath))
                bytes = File.ReadAllBytes(filePath);
        }

        if (bytes == null)
        {
            string fileName = Utils.GetPrefix(originName, ".");
            fileName = fileName.Replace("\\", "/");
            TextAsset asset = LoadAsset<TextAsset>(fileName, Utils.GetSuffix(originName, "."));
            if (asset == null)
                return null;
            bytes = asset.bytes;
        }

        return bytes;
    }

    #endregion

    #region 异步加载

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="assetId"></param>
    /// <param name="callBack"></param>
    public void LoadAssetAsync(int assetId, ObjectCallback callBack)
    {
        CheckIsInitialized();

        if (assetId <= 0)
        {
            Debug.LogErrorFormat("资源ID无效(assetId = {0})", assetId);
            callBack?.Invoke(null);
            return;
        }

        // 从缓存获取
        UnityEngine.Object prefab = GetAssetFromCache(assetId);
        if (prefab != null)
        {
            callBack?.Invoke(prefab);
            return;
        }

        if (enableAssetBundle)
        {
#if USING_ASSET_BUNDLE
            // 从Assetbundle加载
            m_AssetBundleManager.LoadAssetAsync(assetId, (asset) =>
            {
                if (asset == null)
                {
                    LoadAssetAsyncFromResourcesAndAssetDatabase(assetId, callBack);
                    return;
                }
                PutAssetToCache(assetId, asset);
                callBack?.Invoke(asset);
            });
#endif
        }
        else
        {
            // 从Resources加载
            LoadAssetAsyncFromResourcesAndAssetDatabase(assetId, callBack);
        }
    }

    /// <summary>
    /// 从Resources和AssetDatabase加载资源
    /// </summary>
    /// <param name="assetId"></param>
    /// <param name="callBack"></param>
    private void LoadAssetAsyncFromResourcesAndAssetDatabase(int assetId, ObjectCallback callBack)
    {
        //从Resources加载
        m_ResourceManager.LoadAssetAsync(assetId, (asset) =>
        {
#if UNITY_EDITOR
            if (asset == null && !enableAssetBundle)
            {
                // 编辑器下尝试从AssetDatabase加载
                asset = m_AssetDatabaseManager.LoadAsset<Object>(assetId);
            }
#endif
            PutAssetToCache(assetId, asset);
            callBack?.Invoke(asset);
        });
    }

    #endregion

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
