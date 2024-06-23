using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class World
{
    private static readonly string s_MapPath = "Map";

    public string name { get { return m_WorldInfo.name; } }

    private readonly WorldInfo m_WorldInfo = new WorldInfo();

    private ChunkLoader m_ChunkLoader;

    private ChunkProvider m_ChunkProvider;
    public ChunkProvider chunkProvider { get { return m_ChunkProvider; } }

    private RenderWorld m_RenderWorld;

    private PlayerChunkManager m_PlayerChunkManager;

    private bool m_EnableLoadWorldByPlayer = true;

    /// <summary>
    /// 渲染场景
    /// </summary>
    private bool m_EnableRenderWorld = false;
    public bool enableRenderWorld { set { m_EnableRenderWorld = value; } get { return m_EnableRenderWorld; } }

    private WaitForEndOfFrame m_WaitForEndOfFrame = new WaitForEndOfFrame();

    /// <summary>
    /// 加载chunk委托
    /// </summary>
    public System.Action<Chunk> m_OnChunkLoaded = null;

    /// <summary>
    /// 卸载chunk委托
    /// </summary>
    public System.Action<Chunk> m_OnChunkUnload = null;

    public void Destroy()
    {
        if (m_PlayerChunkManager != null)
        {
            m_PlayerChunkManager.Destroy();
            m_PlayerChunkManager = null;
        }

        if (m_ChunkProvider != null)
        {
            m_ChunkProvider.Destroy();
            m_ChunkProvider = null;
        }

        if (m_ChunkLoader != null)
        {
            m_ChunkLoader.Destroy();
            m_ChunkLoader = null;
        }

        if (m_RenderWorld != null)
        {
            m_RenderWorld.Clear();
            m_RenderWorld = null;
        }

        PrefabInfo.Clear();
    }

    public void Load(string filename)
    {
        m_EnableLoadWorldByPlayer = true;

        m_RenderWorld = new RenderWorld(this);
        m_ChunkLoader = new ChunkLoader(this);
        m_ChunkProvider = new ChunkProvider(this, m_ChunkLoader);
        m_PlayerChunkManager = new PlayerChunkManager(this, m_ChunkProvider);

        string path = string.Format("{0}/{1}", s_MapPath, filename);
        m_WorldInfo.Load(path);

        // 创建地形
        if (GameSetting.enableInstancing)
        {
            Dictionary<Vector2Int, MapData>.Enumerator iter = m_WorldInfo.mapDataDict.GetEnumerator();
            while (iter.MoveNext())
            {
                MapData data = iter.Current.Value;
                var instancingTerrain = m_RenderWorld.instancingCore.CreateOrGetInstancingTerrain(iter.Current.Key);
                instancingTerrain.SetMaterials(data.terrainStandard, data.terrainAddStandard, data.terrainLow);
            }
            iter.Dispose();
        }

        PrefabInfo.Load();
    }

    public void Update()
    {
        if (m_EnableLoadWorldByPlayer)
        {
            m_PlayerChunkManager.Update();
            m_ChunkProvider.UnloadQueuedChunks();
        }

        if (m_RenderWorld != null)
            m_RenderWorld.Update();
    }

    public void LateUpdate()
    {
        if (m_EnableRenderWorld && m_RenderWorld != null)
            m_RenderWorld.LateUpdate();
    }

    public void LoadChunkNow(Vector3 pos, int radius, System.Action callback)
    {
        m_EnableLoadWorldByPlayer = false;
        Globals.StartCoroutine(AsyncLoadChunkNow(pos, radius, callback));
    }

    public void LoadChunk(List<ChunkPos> loadChunks, System.Action callback = null)
    {
        m_RenderWorld.NewRenderChunks(loadChunks);
    }

    private IEnumerator AsyncLoadChunkNow(Vector3 pos, int radius, System.Action callback)
    {
        List<ChunkPos> posList = UnityEngine.Pool.ListPool<ChunkPos>.Get();
        ChunkPos chunkPos = Helper.WorldPosToChunkPos(pos);
        for (int z = chunkPos.z - Define.kChunkLoadDistance; z < chunkPos.z + Define.kChunkLoadDistance; ++z)
        {
            for (int x = chunkPos.x - Define.kChunkLoadDistance; x < chunkPos.x + Define.kChunkLoadDistance; ++x)
            {
                ChunkPos loadChunkPos = new ChunkPos(x, z);
                if (!IsOutOfRange(loadChunkPos))
                {
                    m_ChunkProvider.ProvideChunk(loadChunkPos);
                    posList.Add(loadChunkPos);
                }
            }
        }

        m_RenderWorld.NewRenderChunks(posList);
        UnityEngine.Pool.ListPool<ChunkPos>.Release(posList);
        yield return m_WaitForEndOfFrame;

        // 等待加载完成
        do
        {
            yield return m_WaitForEndOfFrame;
        } while (m_RenderWorld.IsNeedToWait());
        yield return m_WaitForEndOfFrame;

        // 通知玩家进入场景
        if (callback != null)
            callback.Invoke();

        // 恢复状态
        m_EnableLoadWorldByPlayer = true;
    }

    public bool GetMapData(ChunkPos pos, out MapData mapData)
    {
        Vector2Int key = Helper.ChunkPosToScenePos(pos);
        if (!m_WorldInfo.TryGetMapData(key, out mapData))
            return false;
        return true;
    }

    #region Chunk


    /// <summary>
    /// 获取chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Chunk GetChunk(ChunkPos pos)
    {
        if (m_ChunkProvider == null || IsOutOfRange(pos))
            return null;
        return m_ChunkProvider.GetChunk(pos);
    }

    public void NotifyChunkLoaded(Chunk chunk)
    {
        if (m_OnChunkLoaded != null)
            m_OnChunkLoaded(chunk);
    }

    public void NotifyChunkUnload(Chunk chunk)
    {
        if (m_OnChunkUnload != null)
            m_OnChunkUnload(chunk);
    }

    /// <summary>
    /// Chunk坐标是否超范围
    /// </summary>
    /// <param name="chunkX"></param>
    /// <param name="chunkZ"></param>
    /// <returns></returns>
    public bool IsOutOfRange(int chunkX, int chunkZ)
    {
        ChunkPos minChunkPos = m_WorldInfo.minChunkPos, maxChunkPos = m_WorldInfo.maxChunkPos;
        return chunkX < minChunkPos.x || chunkX > maxChunkPos.x || chunkZ < minChunkPos.x || chunkZ > maxChunkPos.z;
    }

    /// <summary>
    /// Chunk坐标是否超范围
    /// </summary>
    /// <param name="_chunk_x"></param>
    /// <param name="_chunk_z"></param>
    /// <returns></returns>
    public bool IsOutOfRange(ChunkPos chunkPos)
    {
        return IsOutOfRange(chunkPos.x, chunkPos.z);
    }

    #endregion
}
