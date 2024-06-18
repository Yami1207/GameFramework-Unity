using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class InstancingCore
{
    private static readonly int s_CameraPositionnPropID = Shader.PropertyToID("_CameraPosition");
    private static readonly int s_CameraParamPropID = Shader.PropertyToID("_CameraParam");
    private static readonly int s_CameraFrustumPlanesPropID = Shader.PropertyToID("_CameraFrustumPlanes");

    #region ObjectFactory

    public class ObjectFactory
    {
        public void Destroy()
        {
            m_InstancingDrawcall.Clear();
            m_InstancingRendererPool.Clear();
            m_InstancingChunk.Clear();
            
            //m_InstancingPrefabPool.Clear();
            //m_InstancingTerrain.Clear();

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
    public Vector4[] cameraFrustumPlanes { get { return m_CameraFrustumPlanes; } }

    #endregion

    #region Drawcall

    private ComputeShader m_InstancingDrwacallShader;
    public ComputeShader instancingDrwacallShader { get { return m_InstancingDrwacallShader; } }

    private int m_FrustumCullingKernel = -1;
    public int frustumCullingKernel { get { return m_FrustumCullingKernel; } }

    private int m_MainKernel = -1;
    public int mainKernel { get { return m_MainKernel; } }

    private int m_LODCullingKernel = -1;
    public int lodCullingKernel { get { return m_LODCullingKernel; } }

    #endregion

    /// <summary>
    /// 渲染帧，防止同一帧渲染两次增加内存
    /// </summary>
    private int m_RenderFrame = 0;

    /// <summary>
    /// 所有正在Instacing的chunk
    /// </summary>
    private readonly Dictionary<ChunkPos, InstancingChunk> m_ChunkDict = new Dictionary<ChunkPos, InstancingChunk>();

    /// <summary>
    /// 所有正在Instacing的Terrain
    /// </summary>
    private readonly Dictionary<Vector2Int, InstancingTerrain> m_TerrainDict = new Dictionary<Vector2Int, InstancingTerrain>();

    private readonly Dictionary<int, InstancingRenderer> m_PrefabRendererDict = new Dictionary<int, InstancingRenderer>();

    private readonly List<InstancingRenderer> m_Renderers = new List<InstancingRenderer>();

    /// <summary>
    /// 所有Drawcall
    /// </summary>
    private readonly Dictionary<InstancingDrawcall.Key, InstancingDrawcall> m_DrawcallDict = new Dictionary<InstancingDrawcall.Key, InstancingDrawcall>();

    /// <summary>
    /// 是否需要重新提交Drawcall
    /// </summary>
    private bool m_NeedUploadDrawcall = false;

    /// <summary>
    /// 检查提交Drawcall的Chunk队列
    /// </summary>
    private readonly Queue<InstancingChunk> m_UploadChunkQueue = new Queue<InstancingChunk>();

    /// <summary>
    /// 提交Drawcall的Chunk
    /// </summary>
    private readonly List<InstancingChunk>[] m_UploadChunkBuffer;

    /// <summary>
    /// 当前提交Drawcall的Chunk索引
    /// </summary>
    private int m_UploadChunkBufferIndex = 0;

    /// <summary>
    /// 镜头所在的chunk坐标
    /// </summary>
    private ChunkPos m_CameraPosition;
    public ChunkPos cameraPosition { get { return m_CameraPosition; } }

    public InstancingCore()
    {
        m_UploadChunkBuffer = new List<InstancingChunk>[2];
        for (int i = 0; i < m_UploadChunkBuffer.Length; ++i)
            m_UploadChunkBuffer[i] = new List<InstancingChunk>(256);

        m_InstancingDrwacallShader = AssetManager.instance.LoadAsset<ComputeShader>("Shader/Utils/InstancingDrawcall");
        m_MainKernel = m_InstancingDrwacallShader.FindKernel("Main");
        m_LODCullingKernel = m_InstancingDrwacallShader.FindKernel("LODCulling");
    }

    public void Destroy()
    {
        for (int i = 0; i < m_UploadChunkBuffer.Length; ++i)
            m_UploadChunkBuffer[i].Clear();

        m_PrefabRendererDict.Clear();

        if (m_ChunkDict.Count > 0)
        {
            var iter = m_ChunkDict.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.Clear();
            iter.Dispose();
            m_ChunkDict.Clear();
        }

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

        if (m_ChunkDict.Count == 0)
            return;

        Camera camera = CameraManager.mainCamera;
        if (camera == null)
            return;

        // 获取镜头视锥体
        GeometryUtility.CalculateFrustumPlanes(camera, m_CameraFrustums);
        for (int i = 0; i < 6; ++i)
        {
            var normal = m_CameraFrustums[i].normal;
            var d = m_CameraFrustums[i].distance;
            m_CameraFrustumPlanes[i].Set(normal.x, normal.y, normal.z, d);
        }

        // 镜头坐标
        m_CameraPosition = Helper.WorldPosToChunkPos(CameraManager.mainCamera.transform.position);

        UnityEngine.Profiling.Profiler.BeginSample("PerformInstancingChunk");
        PerformInstancingChunk();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("UploadDrawcall");
        UploadDrawcall();
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("PerformDrawcall");
        PerformDrawcall();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void UploadDrawcall()
    {
        if (!m_NeedUploadDrawcall)
            return;

        // 设置shader参数
        {
            Camera camera = CameraManager.mainCamera;

            // 镜头位置
            m_InstancingDrwacallShader.SetVector(s_CameraPositionnPropID, camera.transform.position);

            // 镜头参数
            Vector4 cameraParam = new Vector4(camera.orthographic ? 0.0f : 1.0f, camera.orthographicSize, camera.fieldOfView, Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView));
            m_InstancingDrwacallShader.SetVector(s_CameraParamPropID, cameraParam);

            // 镜头视锥体平面
            m_InstancingDrwacallShader.SetVectorArray(s_CameraFrustumPlanesPropID, m_CameraFrustumPlanes);
        }

        if (m_Renderers.Count > 0)
        {
            var iter = m_Renderers.GetEnumerator();
            while (iter.MoveNext())
            {
                iter.Current.Reset();
            }
            iter.Dispose();
        }

        var uploadChunks = m_UploadChunkBuffer[m_UploadChunkBufferIndex];
        if (uploadChunks.Count > 0)
        {
            var iter = uploadChunks.GetEnumerator();
            while (iter.MoveNext())
            {
                iter.Current.Perform();
            }
            iter.Dispose();
        }

        m_NeedUploadDrawcall = false;
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
        InstancingChunk chunk;
        if (!m_ChunkDict.TryGetValue(renderChunk.chunkPos, out chunk))
        {
            chunk = m_Factory.CreateInstancingChunk();
            chunk.Init(this, renderChunk.chunkPos);
            m_ChunkDict.Add(renderChunk.chunkPos, chunk);
        }
        return chunk;
    }

    public void DestroyInstancingChunk(InstancingChunk chunk)
    {
        if (m_ChunkDict.Remove(chunk.chunkPos))
        {
            m_NeedUploadDrawcall = true;
            m_Factory.Collect(chunk);
        }
    }

    private void PerformInstancingChunk()
    {
        // 上帧提交Drawcall的Chunk
        var lastUploadChunks = m_UploadChunkBuffer[m_UploadChunkBufferIndex];

        // 当前帧所用的提交chunk索引
        m_UploadChunkBufferIndex = (++m_UploadChunkBufferIndex) % 2;

        // 清除旧数据
        var uploadChunks = m_UploadChunkBuffer[m_UploadChunkBufferIndex];
        uploadChunks.Clear();

        InstancingChunk chunk;
        ChunkPos chunkPos = m_CameraPosition;
        if (!m_ChunkDict.TryGetValue(chunkPos, out chunk))
        {
            var position = Vector3.zero;
            if (DataBridge.GetCurrentPlayerPosition(out position))
            {
                chunkPos = Helper.WorldPosToChunkPos(position);
                if (m_ChunkDict.TryGetValue(chunkPos, out chunk))
                {
                    chunk.renderFrame = m_RenderFrame;
                    m_UploadChunkQueue.Enqueue(chunk);
                }   
            }
        }
        else
        {
            chunk.renderFrame = m_RenderFrame;
            m_UploadChunkQueue.Enqueue(chunk);
        }

        while (m_UploadChunkQueue.Count != 0)
        {
            chunk = m_UploadChunkQueue.Dequeue();
            var status = chunk.status;
            chunk.status = TestPlanesAABB(ref m_CameraFrustums, chunk.bounds);
            if (chunk.status != InstancingChunk.Status.Outside)
                uploadChunks.Add(chunk);

            // 判断是否需要重新设置提交dc数据
            m_NeedUploadDrawcall |= status != chunk.status;

            // 如果chunk可渲染,就需要检查周围chunk
            if (chunk.status != InstancingChunk.Status.Outside)
            {
                chunkPos = chunk.chunkPos;
                if (m_ChunkDict.TryGetValue(new ChunkPos(chunkPos.x - 1, chunkPos.z), out chunk) && chunk.renderFrame != m_RenderFrame)
                {
                    chunk.renderFrame = m_RenderFrame;
                    m_UploadChunkQueue.Enqueue(chunk);
                }
                if (m_ChunkDict.TryGetValue(new ChunkPos(chunkPos.x + 1, chunkPos.z), out chunk) && chunk.renderFrame != m_RenderFrame)
                {
                    chunk.renderFrame = m_RenderFrame;
                    m_UploadChunkQueue.Enqueue(chunk);
                }
                if (m_ChunkDict.TryGetValue(new ChunkPos(chunkPos.x, chunkPos.z - 1), out chunk) && chunk.renderFrame != m_RenderFrame)
                {
                    chunk.renderFrame = m_RenderFrame;
                    m_UploadChunkQueue.Enqueue(chunk);
                }
                if (m_ChunkDict.TryGetValue(new ChunkPos(chunkPos.x, chunkPos.z + 1), out chunk) && chunk.renderFrame != m_RenderFrame)
                {
                    chunk.renderFrame = m_RenderFrame;
                    m_UploadChunkQueue.Enqueue(chunk);
                }
            }
        }

        if (lastUploadChunks.Count > 0)
        {
            // 判断上帧是否有chunk不需要渲染
            for (int i = 0; i < lastUploadChunks.Count; ++i)
            {
                chunk = lastUploadChunks[i];
                if (chunk.renderFrame != m_RenderFrame)
                {
                    chunk.status = InstancingChunk.Status.Outside;
                    m_NeedUploadDrawcall = true;
                }
            }
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

    #region TestPlanesAABB

    private static InstancingChunk.Status TestPlanesAABB(ref Plane[] planes, Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;
        return TestPlanesAABB(ref planes, min, max);
    }

    private static InstancingChunk.Status TestPlanesAABB(ref Plane[] planes, Vector3 minBounds, Vector3 maxBounds, bool testIntersection = true)
    {
        Vector3 min, max;
        var result = InstancingChunk.Status.Inside;

        for (int index = 0; index < 6; ++index)
        {
            var normal = planes[index].normal;
            var planeDistance = planes[index].distance;

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
                return InstancingChunk.Status.Outside;

            if (testIntersection)
            {
                var dot2 = normal.x * max.x + normal.y * max.y + normal.z * max.z;
                if (dot2 + planeDistance <= 0)
                    result = InstancingChunk.Status.Intersect;
            }
        }

        return result;
    }

    #endregion
}
