using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancingChunk
{
    public enum Status
    {
        Unknown,

        // 镜头外
        Outside,

        // 视锥边缘
        Intersect,

        // 镜头内
        Inside,
    }

    private InstancingCore m_InstancingCore;

    private Status m_Status = Status.Unknown;
    public Status status { set { m_Status = value; } get { return m_Status; } }

    /// <summary>
    /// 渲染帧
    /// </summary>
    private int m_RenderFrame = 0;
    public int renderFrame { set { m_RenderFrame = value; } get { return m_RenderFrame; } }

    private ChunkPos m_ChunkPos;
    public ChunkPos chunkPos { get { return m_ChunkPos; } }

    /// <summary>
    /// Chunk包围盒
    /// </summary>
    private Bounds m_Bounds;
    public Bounds bounds { get { return m_Bounds; } }

    public Vector3 chunkCenter { get { return m_Bounds.center; } }

    private Matrix4x4 m_Transform;
    public Matrix4x4 transform { get { return m_Transform; } }

    private bool m_HasExtend = false;
    public bool hasExtend { get { return m_HasExtend; } }

    private InstancingTerrain m_InstancingTerrain;

    private readonly List<InstancingPrefab> m_InstancingPrefabs = new List<InstancingPrefab>();

    public void Init(InstancingCore core, ChunkPos chunkPos)
    {
        m_Status = Status.Unknown;
        m_ChunkPos = chunkPos;
        m_InstancingCore = core;
        m_InstancingTerrain = core.CreateOrGetInstancingTerrain(Helper.ChunkPosToScenePos(chunkPos));
    }

    public void Clear()
    {
        m_Status = Status.Unknown;
        m_RenderFrame = 0;
        m_InstancingTerrain = null;
        m_InstancingCore = null;

        m_InstancingPrefabs.Clear();
    }

    public void Perform()
    {
        if (status == Status.Unknown || status == Status.Outside)
            return;

        // 渲染地形
        m_InstancingTerrain.RenderChunk(this);

        // 镜头距离
        int dx = Mathf.Abs(m_ChunkPos.x - m_InstancingCore.cameraPosition.x);
        int dz = Mathf.Abs(m_ChunkPos.z - m_InstancingCore.cameraPosition.z);

        var iter = m_InstancingPrefabs.GetEnumerator();
        while (iter.MoveNext())
        {
            var prefab = iter.Current;
            if (prefab.visibleDistance == -1 || (prefab.visibleDistance > dx && prefab.visibleDistance > dz))
                prefab.Perform();
        }
        iter.Dispose();
    }

    public void SetBounds(Bounds bounds)
    {
        m_Bounds = bounds;

        m_Transform = Matrix4x4.Translate(bounds.min);
    }

    public void SetExtendDrawcall(bool hasExtend)
    {
        m_HasExtend = hasExtend;
    }

    public void AddPrefab(InstancingPrefab prefab)
    {
        m_InstancingPrefabs.Add(prefab);
    }
}
