using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    private ChunkPos m_ChunkPos;
    public ChunkPos chunkPos { get { return m_ChunkPos; } }

    private Bounds m_Bounds = new Bounds();
    public Bounds bounds { set { m_Bounds = value; } get { return m_Bounds; } }

    private bool m_ExtendDrawcall = false;
    public bool extendDrawcall { set { m_ExtendDrawcall = value; } get { return m_ExtendDrawcall; } }

    /// <summary>
    /// 准备卸载的时间
    /// </summary>
    private long m_PendingUnloadTime;
    public long pendingUnloadTime { set { m_PendingUnloadTime = value; } get { return m_PendingUnloadTime; } }

    public Chunk(int x, int z)
    {
        m_ChunkPos = new ChunkPos(x, z);
    }

    //public void Load(MapData data)
    //{
    //    string directory = SavePath.GetMapDir(data.id, SavePath.MapDirType.ShareMapSave);
    //    if (string.IsNullOrEmpty(directory))
    //        return;

    //    ChunkPos pos = m_ChunkPos - data.pos;
    //    string filename = string.Format("{0}/r.{1}.{2}.data", directory, pos.x, pos.z);
    //    FileStream fileStream = new FileStream(filename, FileMode.Open);
    //    if (fileStream.Length != 0)
    //    {
    //        BinaryReader binaryReader = new BinaryReader(fileStream);

    //        // 包围盒
    //        float minHeight = binaryReader.ReadSingle();
    //        float maxHeight = binaryReader.ReadSingle();
    //        Vector3 min = Helper.ChunkPosToWorld(pos);
    //        Vector3 max = Helper.ChunkPosToWorld(pos + new ChunkPos(1, 1));
    //        m_Bounds.SetMinMax(new Vector3(min.x, minHeight, min.z), new Vector3(max.x, maxHeight, max.z));

    //        // 扩展dc
    //        m_ExtendDrawcall = binaryReader.ReadBoolean();

    //        var bytesSize = Buffer.ByteLength(m_Heights);
    //        byte[] bytes = binaryReader.ReadBytes(bytesSize);
    //        IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(m_Heights, 0);
    //        Marshal.Copy(bytes, 0, ptr, bytesSize);
    //    }
    //    fileStream.Close();
    //}

    public void OnChunkLoaded()
    {
    }
}
