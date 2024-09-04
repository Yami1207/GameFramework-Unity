using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    private ChunkPos m_ChunkPos;
    public ChunkPos chunkPos { get { return m_ChunkPos; } }

    private Bounds m_Bounds = new Bounds();
    public Bounds bounds { set { m_Bounds = value; } get { return m_Bounds; } }

    private bool m_ExtendDrawcall = false;
    public bool extendDrawcall { set { m_ExtendDrawcall = value; } get { return m_ExtendDrawcall; } }

    private int m_WaterHeight = int.MaxValue;
    public int waterHeight { set { m_WaterHeight = value; } get { return m_WaterHeight; } }

    /// <summary>
    /// 准备卸载的时间
    /// </summary>
    private long m_PendingUnloadTime;
    public long pendingUnloadTime { set { m_PendingUnloadTime = value; } get { return m_PendingUnloadTime; } }

    public Chunk(int x, int z)
    {
        m_ChunkPos = new ChunkPos(x, z);
    }

    public void OnChunkLoaded()
    {
    }
}
