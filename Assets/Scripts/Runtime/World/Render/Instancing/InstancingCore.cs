using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class InstancingCore
{
    public static class ShaderConstants
    {
        public static readonly int cameraPositionnPropID = Shader.PropertyToID("_CameraPosition");
        public static readonly int cameraParamPropID = Shader.PropertyToID("_CameraParam");
        public static readonly int cameraFrustumPlanesPropID = Shader.PropertyToID("_CameraFrustumPlanes");

        public static readonly int instancingCountPropID = Shader.PropertyToID("_InstancingCount");

        public static readonly int instancingBufferPropID = Shader.PropertyToID("_InstancingBuffer");

        public static readonly int visibleBufferPropID = Shader.PropertyToID("_VisibleBuffer");

        public static readonly int enableFrustumCullingPropID = Shader.PropertyToID("_EnableFrustumCulling");

        public static readonly int visibleDistancePropID = Shader.PropertyToID("_VisibleDistance");

        public static readonly int instanceMinBoundsPropID = Shader.PropertyToID("_InstanceMinBounds");
        public static readonly int instanceMaxBoundsPropID = Shader.PropertyToID("_InstanceMaxBounds");

        public static readonly int[] LODGroupVisibleBuffer = new int[3]
        {
            Shader.PropertyToID("_VisibleBuffer_LOD0"),
            Shader.PropertyToID("_VisibleBuffer_LOD1"),
            Shader.PropertyToID("_VisibleBuffer_LOD2")
        };

        public static readonly int LODGroupDataPropID = Shader.PropertyToID("_LODGroupData");
    }

    #region ObjectFactory

    public class ObjectFactory
    {
        public void Destroy()
        {
            m_InstancingDrawcall.Clear();
            m_InstancingRendererPool.Clear();
            m_InstancingChunk.Clear();
            m_MatrixList.Clear();
        }

        #region InstancingDrawcall

        private readonly CachePool<InstancingDrawcall> m_InstancingDrawcall = new CachePool<InstancingDrawcall>();

        public InstancingDrawcall CreateInstancingDrawcall()
        {
            return m_InstancingDrawcall.Get();
        }

        public void Collect(InstancingDrawcall obj)
        {
            obj.Clear();
            m_InstancingDrawcall.Release(obj);
        }

        #endregion

        #region InstancingRenderer

        private readonly CachePool<InstancingRenderer> m_InstancingRendererPool = new CachePool<InstancingRenderer>();

        public InstancingRenderer CreateInstancingRenderer()
        {
            return m_InstancingRendererPool.Get();
        }

        public void Collect(InstancingRenderer renderer)
        {
            renderer.Clear();
            m_InstancingRendererPool.Release(renderer);
        }

        #endregion

        #region InstancingChunk

        private readonly CachePool<InstancingChunk> m_InstancingChunk = new CachePool<InstancingChunk>();

        public InstancingChunk CreateInstancingChunk()
        {
            return m_InstancingChunk.Get();
        }

        public void Collect(InstancingChunk obj)
        {
            obj.Clear();
            m_InstancingChunk.Release(obj);
        }

        #endregion

        #region Instancing Prefab

        private readonly CachePool<InstancingPrefab> m_InstancingPrefabPool = new CachePool<InstancingPrefab>();

        public InstancingPrefab CreateInstancingPrefab()
        {
            return m_InstancingPrefabPool.Get();
        }

        public void Collect(InstancingPrefab obj)
        {
            obj.Clear();
            m_InstancingPrefabPool.Release(obj);
        }

        #endregion

        #region List<Matrix4x4>

        private readonly CachePool<List<Matrix4x4>> m_MatrixList = new CachePool<List<Matrix4x4>>();

        public List<Matrix4x4> CreateList_Matrix4x4()
        {
            return m_MatrixList.Get();
        }

        public List<Matrix4x4> CreateList_Matrix4x4(int size)
        {
            var list = m_MatrixList.Get();
            SetListCapacity(list, size);
            return list;
        }

        public void Collect(List<Matrix4x4> list)
        {
            m_MatrixList.Release(list);
        }

        #endregion

        private void SetListCapacity<T>(List<T> list, int size)
        {
            if (list.Capacity < size)
            {
                if (size < 1024)
                    size = UnityEngine.Mathf.NextPowerOfTwo(size);
                list.Capacity = size;
            }
        }
    }

    private readonly ObjectFactory m_Factory = new ObjectFactory();
    public ObjectFactory factory { get { return m_Factory; } }

    #endregion

    #region 视锥属性

    /// <summary>
    /// 镜头视锥
    /// </summary>
    private Plane[] m_CameraFrustums = new Plane[6];

    /// <summary>
    /// 镜头视锥平面（xyz:normal w:distance）
    /// </summary>
    private Vector4[] m_CameraFrustumPlanes = new Vector4[6];

    #endregion

    #region CS相关

    private ComputeShader m_InstancingDrwacallShader;
    public ComputeShader instancingDrwacallShader { get { return m_InstancingDrwacallShader; } }

    private int m_MainKernel = -1;
    public int mainKernel { get { return m_MainKernel; } }

    private int m_LODCullingKernel = -1;
    public int lodCullingKernel { get { return m_LODCullingKernel; } }

    #endregion

    #region 多线程相关

    /// <summary>
    /// Chunk信息列表
    /// </summary>
    private NativeList<InstancingChunkInfo> m_ChunkInfoList = new NativeList<InstancingChunkInfo>(Allocator.Persistent);

    /// <summary>
    /// 当前帧需要渲染的Chunk
    /// </summary>
    private NativeList<int> m_RenderChunks = new NativeList<int>(Allocator.Persistent);

    /// <summary>
    /// 视锥平面
    /// </summary>
    private NativeArray<Vector4> m_FrustumPlanes = new NativeArray<Vector4>(6, Allocator.Persistent);

    #endregion

    /// <summary>
    /// 渲染帧，防止同一帧渲染两次增加内存
    /// </summary>
    private int m_RenderFrame = 0;

    /// <summary>
    /// 所有正在Instacing的chunk列表
    /// </summary>
    private readonly List<InstancingChunk> m_Chunks = new List<InstancingChunk>();

    /// <summary>
    /// 正在Instacing的chunk查找表
    /// </summary>
    private readonly Dictionary<ChunkPos, int> m_ChunkTable = new Dictionary<ChunkPos, int>();

    /// <summary>
    /// 空闲列表的索引
    /// </summary>
    private readonly Queue<int> m_FreeChunkQueue = new Queue<int>();

    /// <summary>
    /// 所有正在Instacing的Terrain
    /// </summary>
    private readonly Dictionary<Vector2Int, InstancingTerrain> m_TerrainDict = new Dictionary<Vector2Int, InstancingTerrain>();

    /// <summary>
    /// 所有正在Instacing的渲染器(预制体)
    /// </summary>
    private readonly Dictionary<int, InstancingRenderer> m_PrefabRendererDict = new Dictionary<int, InstancingRenderer>();

    /// <summary>
    /// 所有正在Instacing的渲染器
    /// </summary>
    private readonly List<InstancingRenderer> m_Renderers = new List<InstancingRenderer>();

    /// <summary>
    /// 所有Drawcall
    /// </summary>
    private readonly Dictionary<InstancingDrawcall.Key, InstancingDrawcall> m_DrawcallDict = new Dictionary<InstancingDrawcall.Key, InstancingDrawcall>();

    /// <summary>
    /// 镜头所在的chunk坐标
    /// </summary>
    private ChunkPos m_CameraPosition;
    public ChunkPos cameraPosition { get { return m_CameraPosition; } }

    public InstancingCore()
    {
        m_InstancingDrwacallShader = AssetManager.instance.LoadAsset<ComputeShader>("Shader/Utils/InstancingDrawcall");
        m_MainKernel = m_InstancingDrwacallShader.FindKernel("Main");
        m_LODCullingKernel = m_InstancingDrwacallShader.FindKernel("LODCulling");
    }

    public void Destroy()
    {
        InstancingRenderer.DestroyBuffer();

        m_ChunkInfoList.Dispose();
        m_RenderChunks.Dispose();
        m_FrustumPlanes.Dispose();

        if (m_ChunkTable.Count > 0)
        {
            var iter = m_ChunkTable.GetEnumerator();
            while (iter.MoveNext())
                m_Chunks[iter.Current.Value].Clear();
            iter.Dispose();
            m_ChunkTable.Clear();
        }

        m_Chunks.Clear();
        m_FreeChunkQueue.Clear();
        m_PrefabRendererDict.Clear();

        if (m_TerrainDict.Count > 0)
        {
            var iter = m_TerrainDict.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.Clear();
            iter.Dispose();
            m_TerrainDict.Clear();
        }

        if (m_Renderers.Count > 0)
        {
            var iter = m_Renderers.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Clear();
            iter.Dispose();
            m_Renderers.Clear();
        }

        if (m_DrawcallDict.Count > 0)
        {
            var iter = m_DrawcallDict.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.Clear();
            iter.Dispose();
            m_DrawcallDict.Clear();
        }

        m_Factory.Destroy();

        System.Array.Clear(m_CameraFrustums, 0, m_CameraFrustums.Length);
        System.Array.Clear(m_CameraFrustumPlanes, 0, m_CameraFrustumPlanes.Length);

        m_RenderFrame = 0;
    }

    public void PerformAll()
    {
        if (m_RenderFrame == Time.renderedFrameCount)
        {
            Debug.LogError("同一帧不能提交两次,否则会占用内存");
            return;
        }
        m_RenderFrame = Time.renderedFrameCount;

        if (m_ChunkTable.Count <= 0)
            return;

        Camera camera = CameraManager.mainCamera;
        if (camera == null)
            return;

        UnityEngine.Profiling.Profiler.BeginSample("SetupCameraData");
        SetupCameraData(camera);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("HandleRenderChunk");
        HandleRenderChunk();
        UnityEngine.Profiling.Profiler.EndSample();

        if (m_RenderChunks.Length <= 0)
            return;

        UnityEngine.Profiling.Profiler.BeginSample("UploadDrawcall");
        UploadDrawcall();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("PerformDrawcall");
        PerformDrawcall();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void SetupCameraData(Camera camera)
    {
        // 镜头坐标
        m_CameraPosition = Helper.WorldPosToChunkPos(camera.transform.position);

        // 获取镜头视锥体
        GeometryUtility.CalculateFrustumPlanes(camera, m_CameraFrustums);
        for (int i = 0; i < 6; ++i)
        {
            var normal = m_CameraFrustums[i].normal;
            var d = m_CameraFrustums[i].distance;
            m_CameraFrustumPlanes[i].Set(normal.x, normal.y, normal.z, d);
        }

        // 镜头位置
        m_InstancingDrwacallShader.SetVector(ShaderConstants.cameraPositionnPropID, camera.transform.position);

        // 镜头参数
        Vector4 cameraParam = new Vector4(camera.orthographic ? 0.0f : 1.0f, camera.orthographicSize, camera.fieldOfView, Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView));
        m_InstancingDrwacallShader.SetVector(ShaderConstants.cameraParamPropID, cameraParam);

        // 镜头视锥体平面
        m_InstancingDrwacallShader.SetVectorArray(ShaderConstants.cameraFrustumPlanesPropID, m_CameraFrustumPlanes);
    }

    private void HandleRenderChunk()
    {
        // 清除上帧数据
        int capacity = Mathf.NextPowerOfTwo(m_ChunkInfoList.Length);
        if (m_RenderChunks.Capacity < capacity)
            m_RenderChunks.SetCapacity(capacity);
        m_RenderChunks.Clear();

        // 视锥平面
        m_FrustumPlanes.CopyFrom(m_CameraFrustumPlanes);

        PerformRenderChunkJob job = new PerformRenderChunkJob(m_ChunkInfoList, m_FrustumPlanes, m_RenderChunks.AsParallelWriter());
        JobHandle jobHandle = job.Schedule(m_ChunkInfoList.Length, 128);
        jobHandle.Complete();
    }

    private void UploadDrawcall()
    {
        if (m_Renderers.Count > 0)
        {
            var iter = m_Renderers.GetEnumerator();
            while (iter.MoveNext())
            {
                iter.Current.Reset();
            }
            iter.Dispose();
        }

        for (int i = 0; i < m_RenderChunks.Length; ++i)
        {
            InstancingChunkInfo info = m_ChunkInfoList[m_RenderChunks[i]];
            if (info.index == -1)
                continue;

            InstancingChunk instancingChunk = m_Chunks[info.index];
            instancingChunk.Perform();
        }
    }

    private void PerformDrawcall()
    {
        if (m_Renderers.Count > 0)
        {
            var iter = m_Renderers.GetEnumerator();
            while (iter.MoveNext())
            {
                iter.Current.Perform();
            }
            iter.Dispose();
        }
    }

    #region Instancing Drawcall

    public InstancingDrawcall CreateInstancingDrawcall(Mesh mesh, Material material, Renderer renderer)
    {
        Debug.Assert(renderer != null);
        return CreateInstancingDrawcall(mesh, material, renderer.shadowCastingMode, renderer.receiveShadows);
    }

    public InstancingDrawcall CreateInstancingDrawcall(Mesh mesh, Material material, ShadowCastingMode shadowCastingMode, bool receiveShadows)
    {
        Debug.Assert(mesh != null);
        Debug.Assert(material != null);

#if UNITY_EDITOR
        InstancingDrawcall.Key key = new InstancingDrawcall.Key() { mesh = mesh.GetInstanceID(), material = material.GetInstanceID() };
        if (m_DrawcallDict.ContainsKey(key))
        {
            Debug.LogErrorFormat("请检查,重复添加drawcal: mesh:{0} material:{1}", mesh.name, material.name);
            return null;
        }
#endif

        var dc = factory.CreateInstancingDrawcall();
        dc.Init(this, mesh, material, shadowCastingMode, receiveShadows);
        m_DrawcallDict.Add(dc.key, dc);
        return dc;
    }

    public void DestroyInstancingDrawcall(InstancingDrawcall dc)
    {
        if (m_DrawcallDict.Remove(dc.key))
            m_Factory.Collect(dc);
    }

    #endregion

    #region Instancing Chunk

    public InstancingChunk CreateOrGetInstancingChunk(RenderChunk renderChunk)
    {
        ChunkPos pos = renderChunk.chunkPos;
        int index;
        if (m_ChunkTable.TryGetValue(pos, out index))
            return m_Chunks[index];

        bool isNewInsertInfo = false;
        InstancingChunk instancingChunk = m_Factory.CreateInstancingChunk();
        if (m_FreeChunkQueue.Count <= 0)
        {
            index = m_Chunks.Count;
            m_Chunks.Add(instancingChunk);
            isNewInsertInfo = true;
        }
        else
        {
            index = m_FreeChunkQueue.Dequeue();
            m_Chunks[index] = instancingChunk;
        }

        instancingChunk.Init(this, renderChunk.chunkPos, index);
        instancingChunk.SetBounds(renderChunk.chunk.bounds);
        instancingChunk.SetExtendDrawcall(renderChunk.chunk.extendDrawcall);

        if (isNewInsertInfo)
            m_ChunkInfoList.Add(instancingChunk.info);
        else
            m_ChunkInfoList[index] = instancingChunk.info;

        // 添加到查找表
        m_ChunkTable.Add(pos, index);

        return instancingChunk;
    }

    public void DestroyInstancingChunk(InstancingChunk chunk)
    {
        ChunkPos pos = chunk.chunkPos;
        int index;
        if (!m_ChunkTable.TryGetValue(pos, out index))
            return;

        // 从查找移除
        m_ChunkTable.Remove(pos);

        // 列表设空
        m_Chunks[index] = null;
        m_ChunkInfoList[index] = InstancingChunkInfo.empty;

        // 添加到空闲列表
        m_FreeChunkQueue.Enqueue(index);
        
        // 回收
        m_Factory.Collect(chunk);
    }

    #endregion

    #region 多线程

    [BurstCompile]
    private struct PerformRenderChunkJob : IJobParallelFor
    {
        [ReadOnly]
        private NativeList<InstancingChunkInfo> m_ChunkInfoList;

        [ReadOnly]
        private NativeArray<Vector4> m_Planes;

        [WriteOnly]
        private NativeList<int>.ParallelWriter m_RenderChunks;

        public PerformRenderChunkJob(NativeList<InstancingChunkInfo> infoList, NativeArray<Vector4> planes, NativeList<int>.ParallelWriter renderChunks)
        {
            m_ChunkInfoList = infoList;
            m_Planes = planes;
            m_RenderChunks = renderChunks;
        }

        public void Execute(int index)
        {
            InstancingChunkInfo info = m_ChunkInfoList[index];
            if (TestPlanesAABB(m_Planes, info.minBounds, info.maxBounds))
                m_RenderChunks.AddNoResize(info.index);
        }

        private bool TestPlanesAABB(NativeArray<Vector4> planes, Vector3 minBounds, Vector3 maxBounds)
        {
            Vector3 min = Vector3.zero, max = Vector3.zero;
            for (int index = 0; index < 6; ++index)
            {
                Vector3 normal = new Vector3(planes[index].x, planes[index].y, planes[index].z);
                float planeDistance = planes[index].w;

                // X axis
                if (normal.x < 0)
                {
                    min.x = minBounds.x;
                    max.x = maxBounds.x;
                }
                else
                {
                    min.x = maxBounds.x;
                    max.x = minBounds.x;
                }

                // Y axis
                if (normal.y < 0)
                {
                    min.y = minBounds.y;
                    max.y = maxBounds.y;
                }
                else
                {
                    min.y = maxBounds.y;
                    max.y = minBounds.y;
                }

                // Z axis
                if (normal.z < 0)
                {
                    min.z = minBounds.z;
                    max.z = maxBounds.z;
                }
                else
                {
                    min.z = maxBounds.z;
                    max.z = minBounds.z;
                }

                var dot1 = normal.x * min.x + normal.y * min.y + normal.z * min.z;
                if (dot1 + planeDistance < 0)
                    return false;
            }

            return true;
        }
    }

    #endregion

    #region Instancing Renderer

    public InstancingRenderer CreateSingleRenderer()
    {
        InstancingRenderer renderer = m_Factory.CreateInstancingRenderer();
        renderer.Init(this);
        AddRenderer(renderer);
        return renderer;
    }

    public InstancingRenderer GetPrefabRenderer(int id)
    {
        InstancingRenderer renderer = null;
        if (!m_PrefabRendererDict.TryGetValue(id, out renderer))
        {
            renderer = m_Factory.CreateInstancingRenderer();
            renderer.Init(this);
            renderer.Load(id);
            AddRenderer(renderer);
            m_PrefabRendererDict.Add(id, renderer);
        }
        return renderer;
    }

    public void AddRenderer(InstancingRenderer renderer)
    {
        m_Renderers.Add(renderer);
    }

    public void RemoveRenderer(InstancingRenderer renderer)
    {
        m_Renderers.Remove(renderer);
        m_Factory.Collect(renderer);
    }

    #endregion

    #region Instancing Terrain

    public InstancingTerrain CreateOrGetInstancingTerrain(Vector2Int pos)
    {
        InstancingTerrain terrain;
        if (!m_TerrainDict.TryGetValue(pos, out terrain))
        {
            terrain = new InstancingTerrain(this);
            m_TerrainDict.Add(pos, terrain);
        }
        return terrain;
    }

    #endregion

    #region Instancing Prefab

    public InstancingPrefab GetInstancingPrefab()
    {
        InstancingPrefab prefab = factory.CreateInstancingPrefab();
        prefab.Init(this);
        return prefab;
    }

    #endregion
}
