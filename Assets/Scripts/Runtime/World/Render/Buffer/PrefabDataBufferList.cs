using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabDataBufferList
{
    private readonly List<PrefabDataBuffer> m_PrefabDataList = new List<PrefabDataBuffer>();
    public List<PrefabDataBuffer> list { get { return m_PrefabDataList; } }

    public PrefabDataBuffer CreateNewBuffer(BufferPool pool)
    {
        var data = pool.RequirePrefabDataBuffer();
        m_PrefabDataList.Add(data);
        return data;
    }

    public void Clear()
    {
        m_PrefabDataList.Clear();
    }
}
