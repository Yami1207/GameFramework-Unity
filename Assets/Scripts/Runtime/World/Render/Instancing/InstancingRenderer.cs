using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancingRenderer
{
    private class DrawcallGroup
    {
        /// <summary>
        /// 屏幕相对高度(LOD数据)
        /// </summary>
        public float screenRelativeTransitionHeight = 1.0f;

        /// <summary>
        /// 包围盒中心点（用于视锥剔除）
        /// </summary>
        public Vector4 boundsCenter;

        /// <summary>
        /// 包围盒范围（用于视锥剔除）
        /// </summary>
        public Vector4 boundsExtent;

        public ComputeBuffer visibleBuffer;

        public InstancingDrawcall drawcall;

        public bool Init(InstancingCore core, LOD lod)
        {
            screenRelativeTransitionHeight = lod.screenRelativeTransitionHeight;

            // 暂不支持多个renderer
            var renderers = lod.renderers;
            return Init(core, renderers[0].gameObject);
        }

        public bool Init(InstancingCore core, GameObject prefab)
        {
            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
            if (meshFilter == null || renderer == null)
                return false;

            boundsCenter = renderer.bounds.center;
            boundsCenter.w = 1.0f;

            boundsExtent = renderer.bounds.extents;
            boundsExtent.w = 0.0f;
            drawcall = core.CreateInstancingDrawcall(meshFilter.sharedMesh, renderer.sharedMaterial, renderer);
            return true;
        }

        public void Clear()
        {
            if (visibleBuffer != null)
                visibleBuffer.Release();

            screenRelativeTransitionHeight = 1.0f;
            visibleBuffer = null;
            drawcall = null;
        }

        public void Render(Bounds bounds)
        {
            if (drawcall != null)
            {
                drawcall.Submit(ref visibleBuffer);
                drawcall.Render(bounds);
            }
        }
    }

    /// <summary>
    /// 最大LOD组数量
    /// </summary>
    private static readonly int s_MaxLOD = 3;

    /// <summary>
    /// InstancingCore实例对象
    /// </summary>
    private InstancingCore m_InstancingCore;

    /// <summary>
    /// 是否进行视锥剔除
    /// </summary>
    private bool m_EnableFrustumCulling = true;
    public bool enableFrustumCulling { set { m_EnableFrustumCulling = value; } get { return m_EnableFrustumCulling; } }

    /// <summary>
    /// 是否进行遮挡剔除
    /// </summary>
    private bool m_EnableOcclusionCulling = false;

    /// <summary>
    /// 是否LOD
    /// </summary>
    private bool m_IsLODInstance = false;

    /// <summary>
    /// LODGroup数据
    /// </summary>
    private Vector4 m_LODGroupData = Vector4.zero;

    /// <summary>
    /// 可视距离
    /// </summary>
    private int m_VisibleDistance = -1;
    public int visibleDistance { get { return m_VisibleDistance + 1; } }

    /// <summary>
    /// 对象变换矩阵列表
    /// </summary>
    private List<Matrix4x4> m_InstanceList;

    /// <summary>
    /// 绘制包围体
    /// </summary>
    private Bounds m_RendererBounds;

    private int m_DrawcallGroupCount = 0;
    private readonly DrawcallGroup[] m_DrawcallGroup;

    private static ComputeBuffer s_InstancingBuffer = null;

    public InstancingRenderer()
    {
        m_DrawcallGroup = new DrawcallGroup[s_MaxLOD];
        for (int i = 0; i < s_MaxLOD; ++i)
            m_DrawcallGroup[i] = new DrawcallGroup();
    }

    public void Init(InstancingCore core)
    {
        m_InstancingCore = core;
        m_RendererBounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        m_InstanceList = core.factory.CreateList_Matrix4x4(1024);
    }

    public void Clear()
    {
        if (m_InstanceList != null)
            m_InstancingCore.factory.Collect(m_InstanceList);

        for (int i = 0; i < s_MaxLOD; ++i)
            m_DrawcallGroup[i].Clear();

        m_InstancingCore = null;
        m_IsLODInstance = false;
        m_LODGroupData = Vector4.zero;
        m_InstanceList = null;
        m_DrawcallGroupCount = 0;
    }

    public void Load(int prefabID)
    {
        Debug.Assert(m_InstancingCore != null);
        Debug.Assert(m_DrawcallGroupCount == 0);

        var info = PrefabInfo.Get(prefabID);
        Debug.Assert(info != null);
        m_VisibleDistance = info.visibleDistance;
        m_EnableOcclusionCulling = info.occlusionCulling;

        GameObject prefab = AssetManager.instance.LoadAsset<GameObject>(info.assertID);
        if (prefab == null)
            return;

        // 判断是否有LODGroup
        LODGroup lodGroup = prefab.GetComponent<LODGroup>();
        if (lodGroup != null)
        {
            m_IsLODInstance = true;
            m_LODGroupData.Set(1.0f, 1.0f, 1.0f, lodGroup.size);

            var lods = lodGroup.GetLODs();
            if (lods.Length == 0)
            {
                // 没有lod
                return;
            }

            float lastScreenRelativeTransitionHeight = 0.999f;
            for (int i = 0; i < lods.Length; ++i)
            {
                if (m_DrawcallGroupCount >= s_MaxLOD)
                {
#if UNITY_EDITOR
                    Debug.LogErrorFormat("asset:{0}中的{1}节点超过可支持的LOD组数量限制. Max:{2}", info.assertID, lodGroup.name, s_MaxLOD);
#endif
                    break;
                }

                var lod = lods[i];
                if (lod.screenRelativeTransitionHeight == 0.999f || lod.screenRelativeTransitionHeight == lastScreenRelativeTransitionHeight)
                {
#if UNITY_EDITOR
                    Debug.LogErrorFormat("asset:{0}中的{1}节点出现无效LOD层. LOD:{2}", info.assertID, lodGroup.name, i);
#endif
                    continue;
                }

                if (m_DrawcallGroup[m_DrawcallGroupCount].Init(m_InstancingCore, lod))
                {
                    lastScreenRelativeTransitionHeight = lod.screenRelativeTransitionHeight;
                    m_LODGroupData[m_DrawcallGroupCount] = lastScreenRelativeTransitionHeight;
                    ++m_DrawcallGroupCount;
                }
            }
        }
        else
        {
            m_IsLODInstance = false;
            if (m_DrawcallGroup[0].Init(m_InstancingCore, prefab))
                m_DrawcallGroupCount = 1;
        }
    }

    public void AddDrawcall(InstancingDrawcall dc)
    {
        Debug.Assert(m_DrawcallGroup[0].drawcall == null);
        m_DrawcallGroup[0].drawcall = dc;

        if (m_DrawcallGroupCount == 0)
            m_DrawcallGroupCount = 1;
    }

    public void Reset()
    {
        m_InstanceList.Clear();
    }

    public void Perform()
    {
        if (m_InstanceList.Count == 0 || m_DrawcallGroupCount == 0)
            return;

        // 创建instancingBuffer
        CreateInstancingBuffer(m_InstanceList.Count);

        // 创建visibleBuffer
        if (m_DrawcallGroup[0].visibleBuffer == null || m_DrawcallGroup[0].visibleBuffer.count < m_InstanceList.Count)
        {
            int count = Mathf.NextPowerOfTwo(m_InstanceList.Count);
            for (int i = 0; i < m_DrawcallGroupCount; ++i)
            {
                if (m_DrawcallGroup[i].visibleBuffer != null)
                    m_DrawcallGroup[i].visibleBuffer.Release();
                m_DrawcallGroup[i].visibleBuffer = new ComputeBuffer(count, 32 * sizeof(float), ComputeBufferType.Append);
                m_DrawcallGroup[i].visibleBuffer.SetCounterValue(0);
            }
        }
        else
        {
            for (int i = 0; i < m_DrawcallGroupCount; ++i)
                m_DrawcallGroup[i].visibleBuffer.SetCounterValue(0);
        }

        ComputeShader cs = m_InstancingCore.instancingDrwacallShader;

        // 设置需要渲染对象数据
        int instancingCount = m_InstanceList.Count;
        s_InstancingBuffer.SetData(m_InstanceList);
        cs.SetInt(InstancingCore.ShaderConstants.instancingCountPropID, instancingCount);

        int kernel = -1;
        if (m_IsLODInstance)
        {
            kernel = m_InstancingCore.GetShaderKernel(InstancingCore.drawLODKernel);
            for (int i = 0; i < m_DrawcallGroupCount; ++i)
                cs.SetBuffer(kernel, InstancingCore.ShaderConstants.LODGroupVisibleBuffer[i], m_DrawcallGroup[i].visibleBuffer);
            cs.SetVector(InstancingCore.ShaderConstants.LODGroupDataPropID, m_LODGroupData);
        }
        else
        {
            kernel = m_InstancingCore.GetShaderKernel(InstancingCore.defaultDrawKernel);
            cs.SetBuffer(kernel, InstancingCore.ShaderConstants.visibleBufferPropID, m_DrawcallGroup[0].visibleBuffer);
        }
        cs.SetBuffer(kernel, InstancingCore.ShaderConstants.instancingBufferPropID, s_InstancingBuffer);

        // 可视距离
        cs.SetFloat(InstancingCore.ShaderConstants.visibleDistancePropID, m_VisibleDistance * Define.kChunkSideLength);

        // 视锥剔除
        cs.SetBool(InstancingCore.ShaderConstants.enableFrustumCullingPropID, m_EnableFrustumCulling);
        if (m_EnableFrustumCulling)
        {
            // 包围体使用首个Group数据
            cs.SetVector(InstancingCore.ShaderConstants.instanceBoundsCenterPropID, m_DrawcallGroup[0].boundsCenter);
            cs.SetVector(InstancingCore.ShaderConstants.instanceBoundsExtentPropID, m_DrawcallGroup[0].boundsExtent);
        }

        // 遮挡剔除
        cs.SetBool(InstancingCore.ShaderConstants.enableOcclusionCullingPropID, HiZCore.instance.isVaild && m_EnableOcclusionCulling);

        // 执行计算
        int threadX = (instancingCount >> 6) + 1;
        cs.Dispatch(kernel, threadX, 1, 1);

        // 提交渲染
        for (int i = 0; i < m_DrawcallGroupCount; ++i)
            m_DrawcallGroup[i].Render(m_RendererBounds);
    }

    public void RequireInstance(Matrix4x4 instance)
    {
        m_InstanceList.Add(instance);
    }

    public void RequireInstances(List<Matrix4x4> instances)
    {
        m_InstanceList.AddRange(instances);
    }

    public static void DestroyBuffer()
    {
        if (s_InstancingBuffer != null)
            s_InstancingBuffer.Release();
        s_InstancingBuffer = null;
    }

    private static void CreateInstancingBuffer(int count)
    {
        if (s_InstancingBuffer != null && s_InstancingBuffer.count < count)
        {
            s_InstancingBuffer.Release();
            s_InstancingBuffer = null;
        }
        if (s_InstancingBuffer == null)
        {
            count = Mathf.NextPowerOfTwo(count);
            s_InstancingBuffer = new ComputeBuffer(count, 16 * sizeof(float));
        }
    }
}
