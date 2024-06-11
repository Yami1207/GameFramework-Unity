using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabBuffer
{
    private readonly Dictionary<int, PrefabDataBufferList> m_PrefabBuffers = new Dictionary<int, PrefabDataBufferList>();

    public PrefabDataBufferList CreateAndGetPrefabDataBufferList(int id, BufferPool pool)
    {
        PrefabDataBufferList list = null;
        if (!m_PrefabBuffers.TryGetValue(id, out list))
        {
            list = pool.RequirePrefabDataBufferList();
            m_PrefabBuffers.Add(id, list);
        }
        return list;
    }

    public Dictionary<int, PrefabDataBufferList>.Enumerator GetEnumerator()
    {
        return m_PrefabBuffers.GetEnumerator();
    }

    public void Clear()
    {
        m_PrefabBuffers.Clear();
    }
}
