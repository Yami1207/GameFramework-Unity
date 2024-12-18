﻿using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkNodePool
{
    private static readonly int s_PoolCapacity = 256;

    public static readonly int s_RenderRootInitialCapacity = 512;

    /// <summary>
    /// 根节点
    /// </summary>
    private ObjectNode m_PoolRoot;

    /// <summary>
    /// RenderChunk根节点
    /// </summary>
    private ObjectNode m_RenderChunkRoot;

    /// <summary>
    /// RenderChunkNode对象池
    /// </summary>
    private readonly CachePool<RenderChunkNode> m_RenderChunkNodePool;

    /// <summary>
    /// ColliderNode对象池
    /// </summary>
    private readonly CachePool<ColliderNode> m_ColliderNodePool;

    /// <summary>
    /// MeshNode对象池
    /// </summary>
    private readonly CachePool<MeshNode> m_MeshNodePool;

    /// <summary>
    /// PrefabNode对象池
    /// </summary>
    private readonly CachePool<PrefabNode> m_PrefabNodePool;

    /// <summary>
    /// WaterNode对象池
    /// </summary>
    private readonly CachePool<WaterNode> m_WaterNodePool;

    public ChunkNodePool()
    {
        m_PoolRoot = new ObjectNode();
        m_RenderChunkRoot = new ObjectNode();

        m_RenderChunkNodePool = new CachePool<RenderChunkNode>(s_PoolCapacity);
        m_ColliderNodePool = new CachePool<ColliderNode>(s_PoolCapacity);
        m_MeshNodePool = new CachePool<MeshNode>(s_PoolCapacity);
        m_PrefabNodePool = new CachePool<PrefabNode>(s_PoolCapacity);
        m_WaterNodePool = new CachePool<WaterNode>(s_PoolCapacity);
    }

    /// <summary>
    /// 创建Pool根节点
    /// </summary>
    public void Init()
    {
        m_PoolRoot.CreateWithStandardPosition("Chunk Pool Root", new Vector3(-100000, -100000, -100000));
        m_PoolRoot.transform.hierarchyCapacity = s_PoolCapacity;

        m_RenderChunkRoot.CreateWithStandardPosition("Render Chunk Root", Vector3.zero);
        m_RenderChunkRoot.transform.hierarchyCapacity = s_RenderRootInitialCapacity;
    }

    public void Clear()
    {
        m_RenderChunkRoot.Destroy();

        m_RenderChunkNodePool.Clear();
        m_ColliderNodePool.Clear();
        m_MeshNodePool.Clear();
        m_PrefabNodePool.Clear();
        m_WaterNodePool.Clear();
    }

    public bool IsRenderChunkRootValid()
    {
        return m_RenderChunkRoot.isValid;
    }

    private bool IsPoolValid()
    {
        return ((m_PoolRoot != null) && (m_PoolRoot.isValid));
    }

    #region Render Chunk Node

    public RenderChunkNode RequireRenderChunkNode(string name)
    {
        if (!IsRenderChunkRootValid())
            return null;

        RenderChunkNode node = m_RenderChunkNodePool.Get();
        node.Create(name);
        node.ChangeParent(m_RenderChunkRoot.transform);
        return node;
    }

    public void Collect(RenderChunkNode node)
    {
        if (node != null)
        {
            if (!IsRenderChunkRootValid())
            {
                node.Destroy();
            }
            else
            {
                // 绑定到PoolRoot
                node.ChangeParent(m_PoolRoot.transform);
                node.PlaceStandardPosition(Vector3.zero, Space.Self);

                // 回收
                m_RenderChunkNodePool.Release(node);
            }
        }
    }

    #endregion

    #region Collider Node

    public ColliderNode RequireColliderNode(string name)
    {
        if (!IsRenderChunkRootValid())
            return null;

        ColliderNode node = m_ColliderNodePool.Get();
        node.Create(name);
        return node;
    }

    public void Collect(ColliderNode node)
    {
        if (!IsPoolValid())
        {
            node.Clear();
            m_ColliderNodePool.Release(node);
        }
        else
        {
            node.Destroy();
        }
    }

    #endregion

    #region Mesh Node

    public MeshNode RequireMeshNode(string name)
    {
        if (!IsRenderChunkRootValid())
            return null;

        MeshNode node = m_MeshNodePool.Get();
        node.Create(name);
        return node;
    }

    public void Collect(MeshNode node)
    {
        if (!IsPoolValid())
        {
            node.Clear();
            m_MeshNodePool.Release(node);
        }
        else
        {
            node.Destroy();
        }
    }

    #endregion

    #region Prefab Node

    public PrefabNode RequirePrefabNode(string name)
    {
        if (!IsRenderChunkRootValid())
            return null;

        var node = m_PrefabNodePool.Get();
        node.Create(name);
        return node;
    }

    public void Collect(PrefabNode node)
    {
        if (!IsPoolValid())
        {
            node.Clear();
            m_PrefabNodePool.Release(node);
        }
        else
        {
            node.Destroy();
        }
    }

    #endregion

    #region Water Node

    public WaterNode RequireWaterNode(string name)
    {
        if (!IsRenderChunkRootValid())
            return null;

        var node = m_WaterNodePool.Get();
        node.Create(name);
        return node;
    }

    public void Collect(WaterNode node)
    {
        if (!IsPoolValid())
        {
            m_WaterNodePool.Release(node);
        }
        else
        {
            node.Destroy();
        }
    }

    #endregion
}
