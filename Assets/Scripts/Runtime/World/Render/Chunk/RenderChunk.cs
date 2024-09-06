using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RenderChunk
{
    private RenderWorld m_RenderWorld;

    private Chunk m_Chunk;
    public Chunk chunk { get { return m_Chunk; } }

    public ChunkPos chunkPos { get { return m_Chunk.chunkPos; } }

    private RenderChunkNode m_RenderChunkNode;
    public RenderChunkNode node { get { return m_RenderChunkNode; } }

    private readonly ChunkNodePool m_ChunkNodePool;
    private readonly RenderChunkPool m_RenderChunkPool;

    private InstancingChunk m_InstancingChunk;

    private float m_SortDistance = float.PositiveInfinity;
    public float sortDistance { set { m_SortDistance = value; } get { return m_SortDistance; } }

    /// <summary>
    /// 是否需要更新
    /// </summary>
    private bool m_IsNeedUpdate = false;

    /// <summary>
    /// 是否需要立即更新(m_IsNeedUpdate == true时有效)
    /// </summary>
    private bool m_IsNeedUpdateNow = false;

    /// <summary>
    /// 是否需要重新提交渲染
    /// </summary>
    private bool m_IsNeedReRerender = false;
    public bool isNeedReRerender { set { m_IsNeedReRerender = value; } get { return m_IsNeedUpdateNow; } }

    /// <summary>
    /// 任务线程锁
    /// </summary>
    private object m_LockCompileTask = new object();

    /// <summary>
    /// 编译时的任务
    /// </summary>
    private RenderChunkCompileTask m_CompileTask;

    /// <summary>
    /// 上次渲染的时刻
    /// </summary>
    public float lastRenderTimeStamp { set; get; }

    public RenderChunk(ChunkNodePool chunkNodePool, RenderChunkPool renderChunkPool)
    {
        m_ChunkNodePool = chunkNodePool;
        m_RenderChunkPool = renderChunkPool;
    }

    public void InitChunk(RenderWorld renderWorld, ChunkPos chunkPos)
    {
        m_RenderWorld = renderWorld;
        m_Chunk = m_RenderWorld.world.GetChunk(chunkPos);
    }

    public void CollectOrDestroy()
    {
        if (m_InstancingChunk != null)
        {
            m_RenderWorld.instancingCore.DestroyInstancingChunk(m_InstancingChunk);
            m_InstancingChunk = null;
        }

        if (m_RenderChunkNode != null)
        {
            m_RenderChunkNode.CollectOrDestroy(m_ChunkNodePool);
            m_RenderChunkNode = null;
        }

        if (m_RenderChunkPool != null)
            m_RenderChunkPool.Collect(this);

        m_RenderWorld = null;
        m_Chunk = null;
        m_SortDistance = float.PositiveInfinity;
    }

    public void LoadChunkData(RenderChunkCompileTask task)
    {
        lock (m_LockCompileTask)
        {
            if (task != m_CompileTask)
            {
                task.Finish(false);
                return;
            }
        }

        // 状态不对,可能主线程删除了这个chunk
        if (task.status != RenderChunkCompileTask.Status.Compiling)
            return;

        if (!m_RenderWorld.world.GetMapData(chunkPos, out var mapData))
            return;

        task.GetRenderChunkCacheData().LoadChunkCache(mapData.id, mapData.pos, m_RenderWorld.world.GetChunk(chunkPos));
    }

    public void RebuildMesh(RenderChunkCompileTask task, RenderChunkDispatcher dispatcher)
    {
        UnityEngine.Debug.Assert(RenderUtil.isMainThread);
        UnityEngine.Debug.Assert(task == m_CompileTask);
        if (task.status != RenderChunkCompileTask.Status.TurnToMainThread)
            return;

        // chunk世界坐标
        var position = Helper.ChunkPosToWorld(chunkPos);

        // 创建chunk节点
        if (m_RenderChunkNode == null)
        {
#if UNITY_EDITOR
            m_RenderChunkNode = m_ChunkNodePool.RequireRenderChunkNode(string.Format("RenderChunk ({0},{1})", chunkPos.x, chunkPos.z));
#else
            m_RenderChunkNode = m_ChunkNodePool.RequireRenderChunkNode("RenderChunk");
#endif
        }
        m_RenderChunkNode.PlaceStandardPosition(position, Space.World);

        // 激活节点
        if (!m_RenderChunkNode.activeSelf)
            m_RenderChunkNode.activeSelf = true;

        // collider
        m_RenderChunkNode.CollectOrDestroyColliderNodes(m_ChunkNodePool);
        var colliderNode = CreateColliderNode(m_RenderChunkNode);
        colliderNode.NewMesh(task.GetRenderChunkCacheData().colliderBuffer);
        colliderNode.SetLayer(TagsAndLayers.TERRAIN_LAYER);

        if (GameSetting.enableInstancing)
        {
            m_InstancingChunk = m_RenderWorld.instancingCore.CreateOrGetInstancingChunk(this);
        }
        else
        {
            if (m_RenderWorld.world.GetMapData(chunkPos, out var mapData))
            {
                m_RenderChunkNode.CollectOrDestroyMeshNodes(m_ChunkNodePool);
                var meshNode = CreateMeshNode(m_RenderChunkNode, "Terrain Mesh Node");
                meshNode.Init(colliderNode.mesh, mapData.terrainStandard, UnityEngine.Rendering.ShadowCastingMode.Off, false);
                meshNode.SetLayer(TagsAndLayers.TERRAIN_LAYER);

                if (chunk.extendDrawcall)
                {
                    meshNode = CreateMeshNode(m_RenderChunkNode, "Terrain Extend Mesh Node");
                    meshNode.Init(colliderNode.mesh, mapData.terrainAddStandard, UnityEngine.Rendering.ShadowCastingMode.Off, false);
                    meshNode.SetLayer(TagsAndLayers.TERRAIN_LAYER);
                }
            }

            if (m_Chunk.waterHeight != int.MinValue)
            {
                m_RenderChunkNode.CollectOrDestroyWaterNodes(m_ChunkNodePool);
                var waterNode = CreateWaterNode(m_RenderChunkNode, "Water");
                waterNode.PlaceStandardPosition(new Vector3(0.0f, 0.001f * m_Chunk.waterHeight, 0.0f), Space.Self);
                waterNode.SetLayer(TagsAndLayers.WATER_LAYER);
            }
        }

        // prefab
        {
            // 回收PrefabNode
            m_RenderChunkNode.CollectOrDestroyPrefabNodes(m_ChunkNodePool);

            var iter = task.GetRenderChunkCacheData().prefabBuffer.GetEnumerator();
            while (iter.MoveNext())
            {
                var list = iter.Current.Value.list;
                if (list.Count == 0)
                    continue;

                int id = iter.Current.Key;
                PrefabInfo info = PrefabInfo.Get(id);
                if (info == null)
                    continue;

                if (GameSetting.enableInstancing && info.useInstancing)
                {
                    var instancingPrefab = m_RenderWorld.instancingCore.GetInstancingPrefab();
                    instancingPrefab.Load(info);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        var data = list[i];
                        instancingPrefab.AddInstance(Matrix4x4.TRS(data.position + position, Quaternion.Euler(data.eulerAngle), data.scale));
                    }
                    m_InstancingChunk.AddPrefab(instancingPrefab);
                }
                else
                {
                    string name = "" + id;
                    for (int i = 0; i < list.Count; ++i)
                    {
                        var data = list[i];
                        var prefabNode = CreatePrefabNode(m_RenderChunkNode, name);
                        prefabNode.SetLayer(info.layer);
                        prefabNode.Load(info);

                        // 设置坐标等信息
                        prefabNode.PlaceStandardPosition(data.position, Space.Self);
                        prefabNode.transform.rotation = Quaternion.Euler(data.eulerAngle);
                        prefabNode.transform.localScale = data.scale;
                    }
                }
            }
            iter.Dispose();
        }
    }

    #region Create Node

    private ColliderNode CreateColliderNode(RenderChunkNode parent)
    {
        ColliderNode node = m_ChunkNodePool.RequireColliderNode("Collider");
        if (!node.activeSelf)
            node.activeSelf = true;
        parent.AddColliderNode(node);
        return node;
    }

    private MeshNode CreateMeshNode(RenderChunkNode parent, string name)
    {
        MeshNode node = m_ChunkNodePool.RequireMeshNode(name);
        if (!node.activeSelf)
            node.activeSelf = true;
        parent.AddMeshNode(node);
        return node;
    }

    private PrefabNode CreatePrefabNode(RenderChunkNode parent, string name)
    {
        PrefabNode node = m_ChunkNodePool.RequirePrefabNode(name);
        if (!node.activeSelf)
            node.activeSelf = true;
        parent.AddPrefabNode(node);
        return node;
    }

    private WaterNode CreateWaterNode(RenderChunkNode parent, string name)
    {
        WaterNode node = m_ChunkNodePool.RequireWaterNode(name);
        if (!node.activeSelf)
            node.activeSelf = true;
        parent.AddWaterNode(node);
        return node;
    }

    #endregion

    #region Need Update

    public void NeedUpdate(bool now)
    {
        if (m_IsNeedUpdate)
            now |= m_IsNeedUpdateNow;

        m_IsNeedUpdate = true;
        m_IsNeedUpdateNow = now;
    }

    /// <summary>
    /// 清除标记
    /// </summary>
    public void ClearNeedUpdate()
    {
        m_IsNeedUpdate = false;
        m_IsNeedUpdateNow = false;
    }

    #endregion

    #region Compile Task

    /// <summary>
    /// 生成RenderChunk任务
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public RenderChunkCompileTask CreateCompileTask(long timeout)
    {
        RenderChunkCompileTask task = null;
        lock (m_LockCompileTask)
        {
            FinishCompileTask();
            m_CompileTask = new RenderChunkCompileTask(this, m_SortDistance);
            task = m_CompileTask;
        }
        return task;
    }

    protected void FinishCompileTask()
    {
        if (m_CompileTask != null && m_CompileTask.status != RenderChunkCompileTask.Status.Done)
            m_CompileTask.Finish(false);
        m_CompileTask = null;
    }

    #endregion
}
