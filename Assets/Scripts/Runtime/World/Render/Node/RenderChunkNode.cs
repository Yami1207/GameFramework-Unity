using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderChunkNode : ObjectNode
{
    private readonly List<ColliderNode> m_ColliderNodes = new List<ColliderNode>();

    private readonly List<MeshNode> m_MeshNodes = new List<MeshNode>();

    private readonly List<PrefabNode> m_PrefabNodes = new List<PrefabNode>();

    private readonly List<WaterNode> m_WaterNodes = new List<WaterNode>();

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

            if (m_WaterNodes.Count > 0)
            {
                for (int i = 0; i < m_WaterNodes.Count; ++i)
                    pool.Collect(m_WaterNodes[i]);
                m_WaterNodes.Clear();
            }

            pool.Collect(this);
        }
        else
        {
            Destroy();
        }
    }

    #endregion

    #region Collider Node

    public void AddColliderNode(ColliderNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_ColliderNodes.Add(node);
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

    #endregion

    #region Mesh Node

    public void AddMeshNode(MeshNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_MeshNodes.Add(node);
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

    #endregion

    #region Prefab Node

    public void AddPrefabNode(PrefabNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_PrefabNodes.Add(node);
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

    #region Water Node

    public void AddWaterNode(WaterNode node)
    {
        node.ChangeParent(transform);
        node.PlaceStandardPosition(Vector3.zero, Space.Self);
        m_WaterNodes.Add(node);
    }

    /// <summary>
    /// 删除/回收WaterNode
    /// </summary>
    /// <param name="pool"></param>
    public void CollectOrDestroyWaterNodes(ChunkNodePool pool)
    {
        if (m_WaterNodes.Count > 0)
        {
            if (pool != null)
            {
                for (int i = 0; i < m_WaterNodes.Count; ++i)
                    pool.Collect(m_WaterNodes[i]);
            }
            else
            {
                for (int i = 0; i < m_WaterNodes.Count; ++i)
                    m_WaterNodes[i].Destroy();
            }

            m_WaterNodes.Clear();
        }
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
