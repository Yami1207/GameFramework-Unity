using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderChunkNode : ObjectNode
{
    private readonly List<ColliderNode> m_ColliderNodes = new List<ColliderNode>();

    private readonly List<MeshNode> m_MeshNodes = new List<MeshNode>();

    private readonly List<PrefabNode> m_PrefabNodes = new List<PrefabNode>();

    #region Collect

    public void CollectOrDestroy(ChunkNodePool pool)
    {
        if (pool != null)
        {
            if (m_ColliderNodes.Count > 0)
            {
                for (int i = 0; i < m_ColliderNodes.Count; ++i)
                    pool.Collect(m_ColliderNodes[i]);
                m_ColliderNodes.Clear();
            }

            if (m_MeshNodes.Count > 0)
            {
                for (int i = 0; i < m_MeshNodes.Count; ++i)
                    pool.Collect(m_MeshNodes[i]);
                m_MeshNodes.Clear();
            }

            if (m_PrefabNodes.Count > 0)
            {
                for (int i = 0; i < m_PrefabNodes.Count; ++i)
                    pool.Collect(m_PrefabNodes[i]);
                m_PrefabNodes.Clear();
            }

            pool.Collect(this);
        }
        else
        {
            Destroy();
        }
    }

    /// <summary>
    /// 删除/回收ColliderNode
    /// </summary>
    /// <param name="pool"></param>
    public void CollectOrDestroyColliderNodes(ChunkNodePool pool)
    {
        if (m_ColliderNodes.Count > 0)
        {
            if (pool != null)
            {
                for (int i = 0; i < m_ColliderNodes.Count; ++i)
                    pool.Collect(m_ColliderNodes[i]);
            }
            else
            {
                for (int i = 0; i < m_ColliderNodes.Count; ++i)
                    m_ColliderNodes[i].Destroy();
            }

            m_ColliderNodes.Clear();
        }
    }

    /// <summary>
    /// 删除/回收MeshNode
    /// </summary>
    /// <param name="pool"></param>
    public void CollectOrDestroyMeshNodes(ChunkNodePool pool)
    {
        if (m_MeshNodes.Count > 0)
        {
            if (pool != null)
            {
                for (int i = 0; i < m_MeshNodes.Count; ++i)
                    pool.Collect(m_MeshNodes[i]);
            }
            else
            {
                for (int i = 0; i < m_MeshNodes.Count; ++i)
                    m_MeshNodes[i].Destroy();
            }

            m_MeshNodes.Clear();
        }
    }

    /// <summary>
    /// 删除/回收PrefabNode
    /// </summary>
    /// <param name="pool"></param>
    public void CollectOrDestroyPrefabNodes(ChunkNodePool pool)
    {
        if (m_PrefabNodes.Count > 0)
        {
            if (pool != null)
            {
                for (int i = 0; i < m_PrefabNodes.Count; ++i)
                    pool.Collect(m_PrefabNodes[i]);
            }
            else
            {
                for (int i = 0; i < m_PrefabNodes.Count; ++i)
                    m_PrefabNodes[i].Destroy();
            }

            m_PrefabNodes.Clear();
        }
    }

    #endregion

    #region Collider Node

    /// <summary>
    /// 是否有碰撞
    /// </summary>
    public bool hasCollider { get { return m_ColliderNodes.Count > 0; } }

    public void AddColliderNode(ColliderNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_ColliderNodes.Add(node);
    }

    #endregion

    #region Mesh Node

    public void AddMeshNode(MeshNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_MeshNodes.Add(node);
    }

    #endregion

    #region Prefab Node

    public void AddPrefabNode(PrefabNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_PrefabNodes.Add(node);
    }

    #endregion

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
        if (m_ColliderNodes.Count > 0)
        {
            for (int i = 0; i < m_ColliderNodes.Count; ++i)
                m_ColliderNodes[i].Destroy();
            m_ColliderNodes.Clear();
        }

        if (m_PrefabNodes.Count > 0)
        {
            for (int i = 0; i < m_PrefabNodes.Count; ++i)
                m_PrefabNodes[i].Destroy();
            m_PrefabNodes.Clear();
        }
    }

    #endregion
}
