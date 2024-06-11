using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderNode : ObjectNode
{
    private MeshCollider m_MeshCollider;
    public MeshCollider meshCollider { get { return m_MeshCollider; } }

    private Mesh m_SelfMesh = null;

    public void NewMesh(ChunkColliderBuffer buffer)
    {
        if (m_SelfMesh == null)
        {
            if (m_MeshCollider != null)
            {
                Mesh newMesh = new Mesh();
#if UNITY_EDITOR
                newMesh.name = "Collider";
#endif
                m_SelfMesh = newMesh;
            }
        }
        if (m_SelfMesh == null)
            return;

        m_SelfMesh.Clear(false);
        m_SelfMesh.subMeshCount = 1;
        m_SelfMesh.SetVertices(buffer.vertices);
        m_SelfMesh.SetTriangles(buffer.indices, 0, false);
        m_SelfMesh.RecalculateBounds();
        m_SelfMesh.UploadMeshData(false);

        SetLayer(TagsAndLayers.kLayerTerrain);
        SetMeshCollider();
    }

    private void SetMeshCollider()
    {
        if (m_MeshCollider != null && m_SelfMesh != null)
            m_MeshCollider.sharedMesh = m_SelfMesh;
    }

    public void ClearMesh()
    {
        if (m_SelfMesh != null)
        {
            // 不需要保持
            m_SelfMesh.Clear(false);
            UnityEngine.Object.Destroy(m_SelfMesh);
            m_SelfMesh = null;

            if (m_MeshCollider != null)
                m_MeshCollider.sharedMesh = null;
        }
    }

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
        m_MeshCollider = base.node.AddComponent<MeshCollider>();
        //m_MeshCollider.skinWidth = 0.001f;
        m_MeshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
        m_MeshCollider = null;

        if (m_SelfMesh != null)
        {
            m_SelfMesh.Clear(false);
            UnityEngine.Object.Destroy(m_SelfMesh);
            m_SelfMesh = null;
        }
    }

    #endregion
}
