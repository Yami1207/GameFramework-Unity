using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class InstancingRenderer
{
    private static class ShaderConstants
    {
        public static readonly int instancingCountPropID = Shader.PropertyToID("_InstancingCount");

        public static readonly int instancingBufferPropID = Shader.PropertyToID("_InstancingBuffer");

        public static readonly int visibleBufferPropID = Shader.PropertyToID("_VisibleBuffer");

        public static readonly int enableFrustumCullingPropID = Shader.PropertyToID("_EnableFrustumCulling");

        public static readonly int visibleDistancePropID = Shader.PropertyToID("_VisibleDistance");

        public static readonly int instanceMinBoundsPropID = Shader.PropertyToID("_InstanceMinBounds");
        public static readonly int instanceMaxBoundsPropID = Shader.PropertyToID("_InstanceMaxBounds");
    }

    private InstancingCore m_InstancingCore;

    private bool m_EnableFrustumCulling = true;
    public bool enableFrustumCulling { set { m_EnableFrustumCulling = value; } get { return m_EnableFrustumCulling; } }

    private int m_VisibleDistance = -1;
    public int visibleDistance { get { return m_VisibleDistance + 1; } }

    private Vector4 m_MinimumBounds = Vector4.zero;
    private Vector4 m_MaximumBounds = Vector4.zero;

    /// <summary>
    /// 对象变换矩阵列表
    /// </summary>
    private List<Matrix4x4> m_InstanceList;

    /// <summary>
    /// 是否重置缓存区
    /// </summary>
    private bool m_ResetInstancingBuffer = false;

    private Bounds m_RendererBounds;

    private ComputeBuffer m_InstancingBuffer;
    private ComputeBuffer m_VisibleBuffer;

    private int m_InstancingBufferSize = 0;
    private int m_VisibleBufferSize = 0;

    private InstancingDrawcall m_InstancingDrawcall;

    public void Init(InstancingCore core)
    {
        m_InstancingCore = core;
        m_RendererBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        m_InstanceList = core.factory.CreateList_Matrix4x4(1024);

        m_ResetInstancingBuffer = true;
    }

    public void Clear()
    {
        if (m_InstanceList != null)
        {
            m_InstancingCore.factory.Collect(m_InstanceList);
            m_InstanceList = null;
        }

        if (m_InstancingBuffer != null)
        {
            m_InstancingBuffer.Release();
            m_InstancingBuffer = null;
        }

        if (m_VisibleBuffer != null)
        {
            m_VisibleBuffer.Release();
            m_VisibleBuffer = null;
        }

        m_InstancingDrawcall = null;
        m_InstancingCore = null;

        m_InstancingBufferSize = 0;
        m_VisibleBufferSize = 0;
    }

    public void Load(int prefabID)
    {
        Debug.Assert(m_InstancingCore != null);
        Debug.Assert(m_InstancingDrawcall == null);
        var info = PrefabInfo.Get(prefabID);
        Debug.Assert(info != null);

        // 可视距离
        m_VisibleDistance = info.visibleDistance;

        GameObject prefab = AssetManager.instance.LoadAsset<GameObject>(info.assertID);
        if (prefab == null)
            return;

        MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
        MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
        if (meshFilter != null && renderer != null)
        {
            m_MinimumBounds = renderer.bounds.min;
            m_MaximumBounds = renderer.bounds.max;
            m_MinimumBounds.w = m_MaximumBounds.w = 1.0f;
            m_InstancingDrawcall = m_InstancingCore.CreateInstancingDrawcall(meshFilter.sharedMesh, renderer.sharedMaterial, renderer);
        }
    }

    public void AddDrawcall(InstancingDrawcall dc)
    {
        m_InstancingDrawcall = dc;
    }

    public void Reset()
    {
        m_ResetInstancingBuffer = true;
        m_InstanceList.Clear();
    }

    public void Perform()
    {
        if (m_InstanceList.Count == 0 || m_InstancingDrawcall == null)
            return;

        ResetInstancingBuffer();
        m_InstancingDrawcall.Render(m_RendererBounds);
    }

    public void RequireInstance(Matrix4x4 instance)
    {
        m_InstanceList.Add(instance);
    }

    public void RequireInstances(List<Matrix4x4> instances)
    {
        m_InstanceList.AddRange(instances);
    }

    private void ResetInstancingBuffer()
    {
        if (!m_ResetInstancingBuffer)
            return;

        ComputeShader cs = m_InstancingCore.instancingDrwacallShader;

        int instanceCount = m_InstanceList.Count;
        if (m_InstancingBuffer != null && m_InstancingBufferSize < instanceCount)
        {
            m_InstancingBuffer.Release();
            m_InstancingBuffer = null;
        }
        if (m_InstancingBuffer == null)
        {
            m_InstancingBufferSize = Mathf.NextPowerOfTwo(instanceCount);
            m_InstancingBuffer = new ComputeBuffer(m_InstancingBufferSize, 16 * sizeof(float));
        }

        if (m_VisibleBuffer != null && m_InstancingBufferSize > m_VisibleBufferSize)
        {
            m_VisibleBuffer.Release();
            m_VisibleBuffer = null;
        }
        if (m_VisibleBuffer == null)
        {
            m_VisibleBufferSize = m_InstancingBufferSize;
            m_VisibleBuffer = new ComputeBuffer(m_VisibleBufferSize, 32 * sizeof(float), ComputeBufferType.Append);
        }
        m_VisibleBuffer.SetCounterValue(0);

        int instancingCount = m_InstanceList.Count;
        int kernel = m_InstancingCore.mainKernel;
        m_InstancingBuffer.SetData(m_InstanceList);
        cs.SetBuffer(kernel, ShaderConstants.instancingBufferPropID, m_InstancingBuffer);
        cs.SetBuffer(kernel, ShaderConstants.visibleBufferPropID, m_VisibleBuffer);
        cs.SetFloat(ShaderConstants.visibleDistancePropID, m_VisibleDistance * Define.kChunkSideLength);
        cs.SetBool(ShaderConstants.enableFrustumCullingPropID, m_EnableFrustumCulling);
        if (m_EnableFrustumCulling)
        {
            cs.SetVector(ShaderConstants.instanceMinBoundsPropID, m_MinimumBounds);
            cs.SetVector(ShaderConstants.instanceMaxBoundsPropID, m_MaximumBounds);
        }
        cs.SetInt(ShaderConstants.instancingCountPropID, instancingCount);
        int threadX = (instancingCount >> 6) + 1;
        cs.Dispatch(kernel, threadX, 1, 1);

        m_InstancingDrawcall.Submit(ref m_VisibleBuffer);

        m_ResetInstancingBuffer = false;
    }
}
