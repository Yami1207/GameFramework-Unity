using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderWorld
{
    public static readonly long kDefaultRenderTimePreFrame = 2;

    private static readonly int kChunkContainerCapacity = 256;

    private World m_World;
    public World world { get { return m_World; } }

    private Dictionary<ChunkPos, RenderChunk> m_RenderChunkDict;

    private BufferPool m_BufferPool;

    private ChunkNodePool m_ChunkNodePool;
    private RenderChunkPool m_RenderChunkPool;

    private RenderChunkDispatcher m_RenderChunkDispatcher;

    /// <summary>
    /// 需要更新RenderChunk的队列(还没开始渲染)
    /// </summary>
    private HashSet<ChunkPos> m_RenderChunkToUpdate;

    /// <summary>
    /// 需要更新RenderChunk排序列表(元素在m_RenderChunkToUpdate也有)
    /// </summary>
    private List<RenderChunk> m_RenderChunkToUpdatePriorityList;

    /// <summary>
    /// 因Finish的RenderChunk需要重新渲染放在下一帧 
    /// </summary>
    private HashSet<ChunkPos> m_NeedRerenderChunkPos = new HashSet<ChunkPos>();
    private List<RenderChunk> m_NeedRerenderChunks = new List<RenderChunk>();

    /// <summary>
    /// 优先列表是否需要更新排序
    /// </summary>
    private bool m_IsResortPriorityList = false;

    /// <summary>
    /// 需要通知完成的列表
    /// </summary>
    protected List<RenderChunk> m_RenderChunkNotifyFinishList;

    /// <summary>
    /// 当渲染的Chunk完成时回调
    /// </summary>
    private System.Action<ChunkPos> m_OnFinishRenderChunk = null;
    public System.Action<ChunkPos> onFinishRenderChunk { set { m_OnFinishRenderChunk = value; } get { return m_OnFinishRenderChunk; } }

    /// <summary>
    /// 管理Instancing的对象
    /// </summary>
    private InstancingCore m_InstancingCore;
    public InstancingCore instancingCore { get { return m_InstancingCore; } }

    private class RenderChunkComparer : IComparer<RenderChunk>
    {
        int IComparer<RenderChunk>.Compare(RenderChunk left, RenderChunk right)
        {
            if (left == right)
                return 0;
            return left.sortDistance == right.sortDistance ? 0 : (left.sortDistance > right.sortDistance ? 1 : -1);
        }
    }
    private static readonly RenderChunkComparer s_RenderChunkComparer = new RenderChunkComparer();

    public RenderWorld(World world)
    {
        m_World = world;

        m_BufferPool = new BufferPool();

        m_ChunkNodePool = new ChunkNodePool();
        m_ChunkNodePool.Init();

        m_RenderChunkDict = new Dictionary<ChunkPos, RenderChunk>(kChunkContainerCapacity);
        m_RenderChunkPool = new RenderChunkPool(this, m_ChunkNodePool);
        m_RenderChunkDispatcher = new RenderChunkDispatcher(this, m_BufferPool);

        m_RenderChunkToUpdate = new HashSet<ChunkPos>();
        m_RenderChunkToUpdatePriorityList = new List<RenderChunk>();
        m_IsResortPriorityList = false;

        m_RenderChunkNotifyFinishList = new List<RenderChunk>(kChunkContainerCapacity);

        if (GameSetting.enableInstancing)
            m_InstancingCore = new InstancingCore(this);
    }

    public void Clear()
    {
        if (m_RenderChunkDispatcher != null)
        {
            m_RenderChunkDispatcher.Destroy();
            m_RenderChunkDispatcher = null;
        }

        if (m_ChunkNodePool != null)
        {
            m_ChunkNodePool.Clear();
            m_ChunkNodePool = null;
        }

        if (m_InstancingCore != null)
        {
            m_InstancingCore.Destroy();
            m_InstancingCore = null;
        }

        m_World = null;
        m_RenderChunkDict = null;
        m_RenderChunkPool = null;

        m_RenderChunkToUpdate.Clear();
        m_RenderChunkToUpdatePriorityList.Clear();
        m_IsResortPriorityList = false;
    }

    public void Update()
    {
        SetupRerenderChunks();

        long startTime = RenderUtil.RealtimeSinceStartupLong();
        long finishTime = RenderUtil.CreateMSFromRealTime(kDefaultRenderTimePreFrame);

        UpdateAllRenderChunk(startTime, finishTime);
    }

    public void LateUpdate()
    {
        if (m_InstancingCore != null)
            m_InstancingCore.PerformAll();
    }

    private void UpdateAllRenderChunk(long startTime, long finishTime)
    {
        // 
        m_RenderChunkDispatcher.RunUploadInMainThreading(finishTime);

        // 通知已完成渲染的RenderChunk
        CommitNotifyRenderChunkFinish(finishTime);

        // 排序
        SortPriorityListIfNeed();

        // 处理RenderChunk
        if (m_RenderChunkToUpdatePriorityList.Count > 0)
        {
            Dictionary<ChunkPos, RenderChunk> removeRenderChunks = null;

            for (int i = 0; i < m_RenderChunkToUpdatePriorityList.Count; ++i)
            {
                // 超时,留到下一帧处理
                if (RenderUtil.IsRealTimeOut(finishTime))
                    break;

                RenderChunk renderChunk = m_RenderChunkToUpdatePriorityList[i];
                m_RenderChunkDispatcher.AddRenderChunk(renderChunk);

                renderChunk.lastRenderTimeStamp = Time.realtimeSinceStartup;
                renderChunk.ClearNeedUpdate();

                if (removeRenderChunks == null)
                    removeRenderChunks = UnityEngine.Pool.DictionaryPool<ChunkPos, RenderChunk>.Get();
                if (!removeRenderChunks.ContainsKey(renderChunk.chunkPos))
                    removeRenderChunks.Add(renderChunk.chunkPos, renderChunk);
            }

            // 移除发起渲染的RenderChunk
            if (removeRenderChunks != null)
            {
                if (removeRenderChunks.Count > 0)
                    InternalRenderChunkRemoveFromUpdate(removeRenderChunks);
                UnityEngine.Pool.DictionaryPool<ChunkPos, RenderChunk>.Release(removeRenderChunks);
            }
        }
    }

    public void NewRenderChunks(List<ChunkPos> posList)
    {
        HashSet<ChunkPos> addChunkPos = UnityEngine.Pool.HashSetPool<ChunkPos>.Get();
        for (int i = 0; i < posList.Count; ++i)
        {
            // 记录新加的位置
            if (!m_RenderChunkDict.ContainsKey(posList[i]))
                addChunkPos.Add(posList[i]);
        }

        // 添加新的section
        HashSet<ChunkPos>.Enumerator iter = addChunkPos.GetEnumerator();
        while (iter.MoveNext())
        {
            AddRenderChunk(iter.Current);
        }
        iter.Dispose();

        // 释放
        UnityEngine.Pool.HashSetPool<ChunkPos>.Release(addChunkPos);
    }

    public void AddRenderChunkToNextFrame(RenderChunk renderChunk)
    {
        if (m_NeedRerenderChunkPos.Add(renderChunk.chunkPos))
            m_NeedRerenderChunks.Add(renderChunk);
    }

    /// <summary>
    /// 添加chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="updateNow"></param>
    private void AddRenderChunk(ChunkPos pos)
    {
        // 判断是否超范围
        if (m_World.IsOutOfRange(pos))
        {
            OnFinishedRenderChunk(pos);
            return;
        }

        // 判断Chunk数据是否加载
        Chunk chunk = m_World.GetChunk(pos);
        if (chunk == null)
        {
            OnFinishedRenderChunk(pos);
            return;
        }

        RenderChunk renderChunk = RequireRenderChunk(pos);
        InternalRenderSectionAddToUpdate(renderChunk);
    }

    /// <summary>
    /// 创建或获取一个Chunk对象
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private RenderChunk RequireRenderChunk(ChunkPos pos)
    {
        RenderChunk renderChunk;
        if (!m_RenderChunkDict.TryGetValue(pos, out renderChunk))
        {
            renderChunk = m_RenderChunkPool.RequireRenderChunk();
            renderChunk.InitChunk(this, pos);
            m_RenderChunkDict.Add(pos, renderChunk);
        }
        return renderChunk;
    }

    private void SetupRerenderChunks()
    {
        if (m_NeedRerenderChunks.Count > 0)
        {
            m_RenderChunkToUpdatePriorityList.AddRange(m_NeedRerenderChunks);
            m_NeedRerenderChunks.Clear();
            m_NeedRerenderChunkPos.Clear();
            m_IsResortPriorityList = true;
        }
    }

    public bool IsNeedToWait()
    {
        return m_NeedRerenderChunks.Count > 0 || m_RenderChunkToUpdatePriorityList.Count > 0 || m_RenderChunkDispatcher.IsNeedToWait();
    }

    #region RenderChunk Update List

    private void InternalRenderSectionAddToUpdate(RenderChunk renderChunk)
    {
        ChunkPos chunkPos = renderChunk.chunkPos;
        if (m_RenderChunkToUpdate.Add(chunkPos))
        {
            Vector3 playerPos;
            DataBridge.GetCurrentPlayerPosition(out playerPos);
            var pos = Helper.WorldPosToChunkPos(playerPos);
            renderChunk.sortDistance = (chunkPos.x - pos.x) * (chunkPos.x - pos.x) + (chunkPos.z - pos.z) * (chunkPos.z - pos.z);

            m_RenderChunkToUpdatePriorityList.Add(renderChunk);
            m_IsResortPriorityList = true;
        }
    }

    /// <summary>
    /// 把RenderChunk从更新队列中删除
    /// </summary>
    /// <param name="removeDict"></param>
    private void InternalRenderChunkRemoveFromUpdate(Dictionary<ChunkPos, RenderChunk> removeDict)
    {
        if (removeDict.Count == m_RenderChunkToUpdate.Count)
        {
            m_RenderChunkToUpdate.Clear();
            m_RenderChunkToUpdatePriorityList.Clear();
            return;
        }

        // 需要遍历删除
        Dictionary<ChunkPos, RenderChunk>.Enumerator iter = removeDict.GetEnumerator();
        while (iter.MoveNext())
        {
            ChunkPos pos = iter.Current.Key;
            RenderChunk renderChunk = iter.Current.Value;
            if (m_RenderChunkToUpdate.Remove(pos))
                m_RenderChunkToUpdatePriorityList.Remove(renderChunk);
        }
        iter.Dispose();
    }

    #endregion 

    #region Priority List

    /// <summary>
    /// 排序优先列表
    /// </summary>
    private void SortPriorityListIfNeed()
    {
        if (m_IsResortPriorityList)
        {
            if (m_RenderChunkToUpdatePriorityList.Count > 1)
                m_RenderChunkToUpdatePriorityList.Sort(s_RenderChunkComparer);

            m_IsResortPriorityList = false;
        }
    }

    #endregion

    #region Notify Render Finish

    public void NotifyRenderChunkFinish(RenderChunk renderChunk)
    {
        m_RenderChunkNotifyFinishList.Add(renderChunk);
    }

    /// <summary>
    /// 提交通知其它系统渲染完成
    /// </summary>
    /// <param name="finishTime"></param>
    private void CommitNotifyRenderChunkFinish(long finishTime)
    {
        var count = m_RenderChunkNotifyFinishList.Count;
        while (count-- > 0)
        {
            if (RenderUtil.IsRealTimeOut(finishTime))
                break;

            ChunkPos pos = m_RenderChunkNotifyFinishList[0].chunkPos;
            OnFinishedRenderChunk(pos);
            m_RenderChunkNotifyFinishList.RemoveAt(0);

            if (count <= 0)
                NotifyAllRenderChunkFinish();
        }
    }

    private void OnFinishedRenderChunk(ChunkPos pos)
    {
        if (m_OnFinishRenderChunk != null)
            m_OnFinishRenderChunk.Invoke(pos);
    }

    /// <summary>
    /// 通知其它系统全部渲染完成
    /// </summary>
    private void NotifyAllRenderChunkFinish()
    {

    }

    #endregion
}
