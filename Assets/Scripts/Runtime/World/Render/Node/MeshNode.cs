using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshNode : ObjectNode
{
    private MeshFilter m_MeshFilter;

    private MeshRenderer m_Renderer;

    public void Init(Mesh mesh, Material material, ShadowCastingMode shadowCastingMode, bool receiveShadows)
    {
        // 设置mesh
        m_MeshFilter.sharedMesh = mesh;

        // 设置渲染器
        m_Renderer.sharedMaterial = material;
        m_Renderer.shadowCastingMode = shadowCastingMode;
        m_Renderer.receiveShadows = receiveShadows;
    }

    public void Clear()
    {
        m_MeshFilter.sharedMesh = null;
        m_Renderer.sharedMaterial = null;
    }

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
        base.OnCreate();

        m_MeshFilter = base.node.AddComponent<MeshFilter>();
        m_Renderer = base.node.AddComponent<MeshRenderer>();
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        m_MeshFilter.sharedMesh = null;
        m_MeshFilter = null;

        m_Renderer.sharedMaterial = null;
        m_Renderer = null;
    }

    #endregion
}
