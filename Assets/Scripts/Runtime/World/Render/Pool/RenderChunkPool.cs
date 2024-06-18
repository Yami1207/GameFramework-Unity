using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static UnityEditor.Rendering.FilterWindow;

public class RenderChunkPool
{
    private readonly RenderWorld m_RenderWorld;

    private readonly ChunkNodePool m_ChunkNodePool;

    private readonly Stack<RenderChunk> m_RenderChunkStack = new Stack<RenderChunk>(ChunkNodePool.s_RenderRootInitialCapacity);

    public RenderChunkPool(RenderWorld renderWorld, ChunkNodePool chunkNodePool)
    {
        m_RenderWorld = renderWorld;
        m_ChunkNodePool = chunkNodePool;
    }

    /// <summary>
    /// 请求一个RenderChunk
    /// </summary>
    /// <returns></returns>
    public RenderChunk RequireRenderChunk()
    {
        RenderChunk result;
        if (m_RenderChunkStack.Count == 0)
            result = new RenderChunk(m_ChunkNodePool, this);
        else
            result = m_RenderChunkStack.Pop();
        return result;
    }

    /// <summary>
    /// 回收一个RenderChunk
    /// </summary>
    /// <param name="element"></param>
    public void Collect(RenderChunk element)
    {
        m_RenderChunkStack.Push(element);
    }
}
