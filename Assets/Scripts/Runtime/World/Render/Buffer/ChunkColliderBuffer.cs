using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ChunkColliderBuffer
{
    private readonly Vector3[] m_Vertices;
    public Vector3[] vertices { get { return m_Vertices; } }

    private readonly int[] m_Indices;
    public int[] indices { get { return m_Indices; } }

    public ChunkColliderBuffer()
    {
        // 地形为16 * 16个正方形组成的模型
        // 模型顶点数为17 * 17
        // 模型顶点索引数为16 * 16 * 2
        m_Vertices = new Vector3[(Define.kChunkSideLength + 1) * (Define.kChunkSideLength + 1)];
        m_Indices = new int[(Define.kChunkSideLength * Define.kChunkSideLength << 1) * 3];

        // 初始化顶点索引
        int index = 0;
        for (int i = 0; i < Define.kChunkSideLength; ++i)
        {
            int low = i * (Define.kChunkSideLength + 1), high = (i + 1) * (Define.kChunkSideLength + 1);
            for (int j = 0; j < Define.kChunkSideLength; ++j)
            {
                int p1 = low + j, p2 = p1 + 1;
                int p3 = high + j, p4 = p3 + 1;
                m_Indices[index] = p1;
                m_Indices[index + 1] = p3;
                m_Indices[index + 2] = p4;

                m_Indices[index + 3] = p1;
                m_Indices[index + 4] = p4;
                m_Indices[index + 5] = p2;

                index += 6;
            }
        }
    }
}
