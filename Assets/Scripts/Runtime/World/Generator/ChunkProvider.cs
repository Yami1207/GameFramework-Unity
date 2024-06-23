using System.Collections;
using System.Collections.Generic;
//using UnityEngine;

public class ChunkProvider
{
    private World m_World;

    private ChunkLoader m_ChunkLoader;

    private readonly Dictionary<ChunkPos, Chunk> m_ChunkCacheDict = new Dictionary<ChunkPos, Chunk>();

    /// <summary>
    /// 每帧卸载chunk个数
    /// </summary>
    private int m_MaxUnloadCountPerFrame = 5;

    /// <summary>
    /// 准备卸载的Chunk列表
    /// </summary>
    private readonly Queue<Chunk> m_PendingUnloadQueue = new Queue<Chunk>();

    public ChunkProvider(World world, ChunkLoader chunkLoader)
    {
        m_World = world;
        m_ChunkLoader = chunkLoader;
    }

    public void Destroy()
    {
        m_ChunkLoader = null;
        m_World = null;
    }

    public Dictionary<ChunkPos, Chunk>.Enumerator GetCacheChunkEnumerator()
    {
        return m_ChunkCacheDict.GetEnumerator();
    }

    /// <summary>
    /// 提供Chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Chunk ProvideChunk(ChunkPos pos)
    {
        return LoadChunk(pos);
    }

    /// <summary>
    /// 加载Chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Chunk LoadChunk(ChunkPos pos)
    {
        //从缓存中获取
        Chunk chunk = GetCacheChunk(pos);
        if (chunk == null)
        {
            //从本地文件读取
            chunk = LoadChunkFromFile(pos);
            if (chunk != null)
            {
                PutChunkToCache(chunk);
            }
        }
        return chunk;
    }

    private Chunk LoadChunkFromFile(ChunkPos pos)
    {
        return m_ChunkLoader.LoadChunk(pos);
    }

    public Chunk GetChunk(ChunkPos pos)
    {
        return LoadChunk(pos);
    }

    private Chunk GetCacheChunk(ChunkPos pos)
    {
        Chunk chunk = null;
        if (m_ChunkCacheDict.TryGetValue(pos, out chunk))
        {
            if (chunk != null)
            {
                // 取消卸载标志
                chunk.pendingUnloadTime = 0;
            }

        }
        return chunk;
    }

    private void PutChunkToCache(Chunk chunk)
    {
        ChunkPos chunkPos = chunk.chunkPos;
        bool isLoaded = m_ChunkCacheDict.ContainsKey(chunkPos);
        m_ChunkCacheDict[chunkPos] = chunk;

        if (!isLoaded)
            chunk.OnChunkLoaded();
    }

    #region Unload chunk

    /// <summary>
    /// 添加准备卸载的chunk
    /// </summary>
    /// <param name="chunk"></param>
    public void AddPendingUnloadChunk(Chunk chunk)
    {
        if (chunk != null)
        {
            m_PendingUnloadQueue.Enqueue(chunk);
            chunk.pendingUnloadTime = RenderUtil.RealtimeSinceStartupLong() + Define.kChunkAliveTime;
        }
    }

    public void UnloadQueuedChunks()
    {
        int unloadCount = 0;
        while (m_PendingUnloadQueue.Count > 0 && unloadCount < m_MaxUnloadCountPerFrame)
        {
            Chunk chunk = m_PendingUnloadQueue.Peek();

            //取消卸载
            if (chunk.pendingUnloadTime == 0)
            {
                m_PendingUnloadQueue.Dequeue();
                continue;
            }

            //未到卸载时间
            if (chunk.pendingUnloadTime > RenderUtil.RealtimeSinceStartupLong())
            {
                //由于按时间顺序入队，所以这里可以直接跳出循环。
                break;
            }

            //出队
            m_PendingUnloadQueue.Dequeue();
            if (UnloadChunk(chunk))
                ++unloadCount;
        }
    }

    private bool UnloadChunk(Chunk chunk)
    {
        bool success = m_ChunkCacheDict.Remove(chunk.chunkPos);
        if (success)
            m_World.NotifyChunkUnload(chunk);
        return success;
    }

    #endregion
}
