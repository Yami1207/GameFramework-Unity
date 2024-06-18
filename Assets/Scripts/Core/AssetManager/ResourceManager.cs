using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static AssetManager;

public class ResourceManager : IAssetLoader
{
    private class AssetLoaderTask
    {
        public AssetInfo info;
        public ObjectCallback callBack;

        public AssetLoaderTask(AssetInfo _info, ObjectCallback _callBack)
        {
            info = _info;
            callBack = _callBack;
        }
    }

    private WaitForEndOfFrame m_WaitForEndOfFrame = new WaitForEndOfFrame();

    /// <summary>
    /// 最大同时异步加载数量
    /// </summary>
    private static readonly int s_MaxAsyncLoadingCount = 2;

    /// <summary>
    /// 当前加载数量
    /// </summary>
    private int m_LoadingCount = 0;

    /// <summary>
    /// 待执行的任务队列
    /// </summary>
    private Queue<AssetLoaderTask> m_LoaderTaskQueue = new Queue<AssetLoaderTask>();

    public void Init()
    {
    }

    public void DoExitScene()
    {
        // 停止正在异步加载的任务
        m_LoaderTaskQueue.Clear();
        m_LoadingCount = 0;
    }

    #region 同步加载

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

    #endregion

    #region 异步加载

    public void LoadAssetAsync(int assetId, AssetManager.ObjectCallback callBack)
    {
        AssetInfo info = AssetInfo.GetAssetInfo(assetId);
        if (info == null)
        {
            callBack?.Invoke(null);
            return;
        }

        if (m_LoadingCount < s_MaxAsyncLoadingCount)
        {
            StartLoadAsync(info.resourcesPath, callBack);
        }
        else
        {
            //添加异步加载任务
            AssetLoaderTask task = new AssetLoaderTask(info, callBack);
            m_LoaderTaskQueue.Enqueue(task);
        }
    }

    /// <summary>
    /// 启动异步加载任务
    /// </summary>
    /// <param name="resourcesPath"></param>
    /// <param name="callBack"></param>
    /// <param name="func"></param>
    private void StartLoadAsync(string resourcesPath, ObjectCallback callBack)
    {
        ++m_LoadingCount;

        CoroutineRunner.Run(LoadAssetCoroutine(resourcesPath, (asset) =>
        {
            callBack?.Invoke(asset);
            OnLoadFinishAndCheckNext();
        }));
    }

    private IEnumerator LoadAssetCoroutine(string path, ObjectCallback callBack)
    {
        ResourceRequest request = Resources.LoadAsync(path);
        float startTime = Time.time;

        const float kTimeout = 10.0f;
        while (request.isDone == false)
        {
            if ((Time.time - startTime) >= kTimeout)
                break;
            yield return m_WaitForEndOfFrame;
        }
        UnityEngine.Object prefab = request.asset;
        callBack?.Invoke(prefab);
    }

    private void OnLoadFinishAndCheckNext()
    {
        if (m_LoadingCount > 0)
            --m_LoadingCount;

        if (m_LoadingCount < s_MaxAsyncLoadingCount && m_LoaderTaskQueue.Count > 0)
        {
            AssetLoaderTask task = m_LoaderTaskQueue.Dequeue();
            StartLoadAsync(task.info.resourcesPath, task.callBack);
        }
    }

    #endregion
}
