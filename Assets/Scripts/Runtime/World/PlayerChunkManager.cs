using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChunkManager
{
    private World m_World;

    private ChunkProvider m_ChunkProvider;

    /// <summary>
    /// 需要渲染的chunk
    /// </summary>
    private readonly List<ChunkPos> m_NeedRenderChunkList = new List<ChunkPos>();

    //        /// <summary>
    //        /// 上个渲染点的位置
    //        /// </summary>
    //        private Vector3Int m_LastPos = new Vector3Int(-1000000, -1000000, -1000000);

    private ChunkPos m_LastChunkPos = new ChunkPos(-10000, -10000);

    /// <summary>
    /// 是否需要更新渲染
    /// </summary>
    private bool m_SendToUpdateRender = false;

    public PlayerChunkManager(World world, ChunkProvider chunkProvider)
    {
        m_World = world;
        m_ChunkProvider = chunkProvider;
    }

    public void Destroy()
    {
        m_World = null;
        m_ChunkProvider = null;

        m_NeedRenderChunkList.Clear();
    }

    public void Update()
    {
        //检测玩家移动
        CheckPlayerMoving();

        if (m_SendToUpdateRender)
        {
            m_SendToUpdateRender = false;

            if (m_NeedRenderChunkList.Count > 0)
            {
                m_World.LoadChunk(m_NeedRenderChunkList);
                m_NeedRenderChunkList.Clear();
            }
        }

        if (CheckFinishRender())
            CheckChunkToUnload();
    }

    /// <summary>
    /// 更新玩家移动
    /// </summary>
    private void CheckPlayerMoving()
    {
        Vector3 playerPos;
        if (DataBridge.GetCurrentPlayerPosition(out playerPos))
        {
            Vector3Int pos = new Vector3Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y), Mathf.FloorToInt(playerPos.z));
            ChunkPos chunkPos = Helper.WorldPosToChunkPos(pos);
            SubmitChunkToLoad(chunkPos);
            OnPlayerChunkPosMoved(chunkPos);
        }
    }

    /// <summary>
    /// 确保加载玩家附近的数据
    /// </summary>
    /// <param name="chunkPos"></param>
    private void SubmitChunkToLoad(ChunkPos chunkPos)
    {
        int range = 1;
        int minX = chunkPos.x - range, maxX = chunkPos.x + range;
        int minZ = chunkPos.z - range, maxZ = chunkPos.z + range;
        for (int x = minX; x <= maxX; ++x)
        {
            for (int z = minZ; z <= maxZ; ++z)
                m_World.GetChunk(x, z);
        }
    }

    /// <summary>
    /// 检查是否需要渲染
    /// </summary>
    /// <param name="chunkPos"></param>
    private void CheckNeedRender(ChunkPos chunkPos)
    {
        m_NeedRenderChunkList.Add(chunkPos);
    }

    private void OnPlayerChunkPosMoved(ChunkPos chunkPos)
    {
        ChunkPos diff = m_LastChunkPos - chunkPos;
        if (diff.x == 0 && diff.z == 0)
            return;

        int loadChunkRadius = Define.kChunkLoadDistance;
        m_LastChunkPos = chunkPos;
        for (int x = -loadChunkRadius; x <= loadChunkRadius; ++x)
        {
            for (int z = -loadChunkRadius; z <= loadChunkRadius; ++z)
                CheckNeedRender(new ChunkPos(x + m_LastChunkPos.x, z + m_LastChunkPos.z));
        }

        MarkSendToRender();
    }

    /// <summary>
    /// 检查是否已完成渲染
    /// </summary>
    private bool CheckFinishRender()
    {
        //int dist = m_PlayerLogicChunkRadius;
        //for (int x = -dist; x <= dist; ++x)
        //{
        //    for (int z = -dist; z <= dist; ++z)
        //    {
        //        var chunkPos = new ChunkPos(x + m_LastChunkPos.x, z + m_LastChunkPos.z);
        //        if (m_World.renderWorld.IsNeedChunkData(chunkPos))
        //            return false;
        //    }
        //}
        return true;
    }

    /// <summary>
    /// 检查卸载chunk
    /// </summary>
    private void CheckChunkToUnload()
    {
        int unloadChunkDistance = Define.kChunkUnloadDistance;

        var iter = m_ChunkProvider.GetCacheChunkEnumerator();
        while (iter.MoveNext())
        {
            Chunk chunk = iter.Current.Value;
            if (chunk != null)
            {
                ChunkPos chunkPos = chunk.chunkPos;

                // 判断chunk是否卸载
                if (!IsInRange(m_LastChunkPos, chunkPos, unloadChunkDistance))
                {
                    if (Mathf.Abs(chunk.pendingUnloadTime) > Mathf.Epsilon)
                        continue;
                    m_ChunkProvider.AddPendingUnloadChunk(chunk);
                }
                else
                {
                    chunk.pendingUnloadTime = 0;
                }
            }
        }
        iter.Dispose();
    }

    private void MarkSendToRender()
    {
        m_SendToUpdateRender = true;
    }

    private static bool IsInRange(ChunkPos chunkPos, ChunkPos otherChunkPos, int dist)
    {
        var diff = chunkPos - otherChunkPos;
        return diff.x <= dist && diff.x >= -dist && diff.z <= dist && diff.z >= -dist;
    }
}
