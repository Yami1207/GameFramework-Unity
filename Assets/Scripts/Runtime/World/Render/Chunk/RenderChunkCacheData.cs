using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RenderChunkCacheData
{
    private Bounds m_Bounds = new Bounds();

    private readonly BufferPool m_BufferPool;

    private readonly ChunkColliderBuffer m_ColliderBuffer = new ChunkColliderBuffer();
    public ChunkColliderBuffer colliderBuffer { get { return m_ColliderBuffer; } }

    private PrefabBuffer m_PrefabBuffer;
    public PrefabBuffer prefabBuffer { get { return m_PrefabBuffer; } }

    public RenderChunkCacheData(BufferPool pool)
    {
        m_BufferPool = pool;
    }

    /// <summary>
    /// 清空旧数据
    /// </summary>
    public void Clear()
    {
        if (m_PrefabBuffer != null)
        {
            m_BufferPool.Collect(m_PrefabBuffer);
            m_PrefabBuffer = null;
        }
    }

    public void LoadChunkCache(int mapID, ChunkPos origin, Chunk chunk)
    {
        if (chunk == null)
            return;

        string directory = SavePath.GetMapDir(mapID, SavePath.MapDirType.ShareMapSave);
        if (string.IsNullOrEmpty(directory))
            return;

        ChunkPos pos = chunk.chunkPos - origin;
        string filename = string.Format("{0}/r.{1}.{2}.data", directory, pos.x, pos.z);
        FileStream fileStream = new FileStream(filename, FileMode.Open);
        if (fileStream.Length != 0)
        {
            BinaryReader binaryReader = new BinaryReader(fileStream);

            // 包围盒
            float minHeight = binaryReader.ReadSingle();
            float maxHeight = binaryReader.ReadSingle();
            Vector3 min = Helper.ChunkPosToWorld(chunk.chunkPos);
            Vector3 max = Helper.ChunkPosToWorld(chunk.chunkPos + new ChunkPos(1, 1));
            m_Bounds.SetMinMax(new Vector3(min.x, minHeight, min.z), new Vector3(max.x, maxHeight, max.z));
            chunk.bounds = m_Bounds;

            // 扩展dc
            chunk.extendDrawcall = binaryReader.ReadBoolean();

            // 地形mesh数据
            for (int z = 0; z <= Define.kChunkSideLength; ++z)
            {
                int index = z * (Define.kChunkSideLength + 1);
                for (int x = 0; x <= Define.kChunkSideLength; ++x)
                    m_ColliderBuffer.vertices[index + x] = new Vector3(x, binaryReader.ReadSingle(), z);
            }

            m_PrefabBuffer = m_BufferPool.RequirePrefabBuffer();
            LoadVegetationData(binaryReader);
        }
        fileStream.Close();
    }

    /// <summary>
    /// 加载植被数据
    /// </summary>
    /// <param name="reader"></param>
    private void LoadVegetationData(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        for (int i = 0; i < count; ++i)
        {
            int id = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float scaleXZ = reader.ReadSingle();
            float scaleY = reader.ReadSingle();
            float rotationY = Mathf.Rad2Deg * reader.ReadSingle();

            PrefabDataBufferList list = m_PrefabBuffer.CreateAndGetPrefabDataBufferList(id, m_BufferPool);
            PrefabDataBuffer data = list.CreateNewBuffer(m_BufferPool);
            data.position = new Vector3(x, y, z);
            data.scale = new Vector3(scaleXZ, scaleY, scaleXZ);
            data.eulerAngle = new Vector3(0, rotationY, 0);
        }
    }
}
