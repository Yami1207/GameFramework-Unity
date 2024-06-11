using System.Collections;
using System.Collections.Generic;
//using UnityEngine;

public class ChunkProvider
{
    private World m_World;

    private ChunkLoader m_ChunkLoader;

    private readonly Dictionary<int, Chunk> m_ChunkCacheDict = new Dictionary<int, Chunk>();

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

    public Dictionary<int, Chunk>.Enumerator GetCacheChunkEnumerator()
    {
        return m_ChunkCacheDict.GetEnumerator();
    }

    /// <summary>
    /// 提供Chunk
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public Chunk ProvideChunk(int x, int z)
    {
        return LoadChunk(x, z);
    }

    /// <summary>
    /// 加载Chunk
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Chunk LoadChunk(int x, int z)
    {
        //从缓存中获取
        Chunk chunk = GetCacheChunk(x, z);
        if (chunk == null)
        {
            //从本地文件读取
            chunk = LoadChunkFromFile(x, z);
            if (chunk != null)
            {
                PutChunkToCache(chunk);
            }
        }
        return chunk;
    }

    private Chunk LoadChunkFromFile(int x, int z)
    {
        return m_ChunkLoader.LoadChunk(x, z);
    }

    public Chunk GetChunk(int x, int z)
    {
        return LoadChunk(x, z);
    }

    private Chunk GetCacheChunk(int x,int z)
    {
        int hash = Helper.GetHashCode(x, z);
        Chunk chunk = null;
        if (m_ChunkCacheDict.TryGetValue(hash, out chunk))
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

        bool isLoaded = m_ChunkCacheDict.ContainsKey(chunkPos.hashCode);
        m_ChunkCacheDict[chunkPos.hashCode] = chunk;

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
        bool success = m_ChunkCacheDict.Remove(chunk.chunkPos.hashCode);
        if (success)
            m_World.NotifyChunkUnload(chunk);
        return success;
    }

    #endregion
}

//namespace Framework
//{
//    /// <summary>
//    /// Chunk提供器
//    /// </summary>
//    public sealed class ChunkProvider
//    {
//        private World m_World;

//        /// <summary>
//        /// Chunk生成器
//        /// </summary>
//        private ChunkGenerator m_ChunkGenerator;

//        /// <summary>
//        /// Chunk加载器
//        /// </summary>
//        private ChunkLoader m_ChunkLoader;

//        /// <summary>
//        /// Chunk缓存
//        /// </summary>
//        private readonly Dictionary<int, Chunk> m_ChunkCacheDict = new Dictionary<int, Chunk>();





//        public ChunkProvider(World world, ChunkLoader loader, ChunkGenerator generator)
//        {
//            m_World = world;
//            m_ChunkLoader = loader;
//            m_ChunkGenerator = generator;
//        }

//        public void Destroy()
//        {
//            if (m_ChunkGenerator == null)
//            {
//                m_ChunkGenerator.Destroy();
//                m_ChunkGenerator = null;
//            }

//            if (m_ChunkLoader == null)
//            {
//                m_ChunkLoader.Destroy();
//                m_ChunkLoader = null;
//            }

//            m_World = null;
//        }









//        private Chunk GetCacheChunk(int x, int z)
//        {
//            int hash = Helper.GetChunkPosHashCode(x, z);
//            Chunk chunk = null;
//            if (m_ChunkCacheDict.TryGetValue(hash, out chunk))
//            {
//                if (chunk != null)
//                    chunk.pendingUnloadTime = 0.0f;
//            }
//            return chunk;
//        }

//        /// <summary>
//        /// 加载Chunk
//        /// </summary>
//        /// <param name="x"></param>
//        /// <param name="z"></param>
//        /// <returns></returns>
//        private Chunk LoadChunk(int x, int z)
//        {
//            //从缓存中获取
//            Chunk chunk = this.GetCacheChunk(x, z);
//            if (chunk == null)
//            {

//            }
//            return chunk;
//        }

//        private void PutChunkToCache(Chunk chunk)
//        {
//            ChunkPos pos = chunk.chunkPos;
//            bool isFirstLoad = !m_ChunkCacheDict.ContainsKey(pos.hashCode);
//            m_ChunkCacheDict[pos.hashCode] = chunk;

//            if (isFirstLoad)
//            {
//                // 标记chunk已加载
//                chunk.OnChunkLoaded();

//                // 通知chunk已加载
//                m_World.NotifyChunkLoaded(chunk);
//            }
//        }


//    }
//}
