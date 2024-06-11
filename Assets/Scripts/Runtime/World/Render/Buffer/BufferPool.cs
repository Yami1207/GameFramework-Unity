using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferPool
{
    #region PrefabBuffer

    private readonly CachePool<PrefabBuffer> m_PrefabBufferCache = new CachePool<PrefabBuffer>();

    public PrefabBuffer RequirePrefabBuffer()
    {
        lock (m_PrefabBufferCache)
        {
            PrefabBuffer buffer = m_PrefabBufferCache.Get();
            return buffer;
        }
    }

    public void Collect(PrefabBuffer obj)
    {
        // 回收数据
        var iter = obj.GetEnumerator();
        while (iter.MoveNext())
        {
            Collect(iter.Current.Value);
        }
        iter.Dispose();

        // 清空引用
        obj.Clear();

        lock (m_PrefabBufferCache)
        {
            m_PrefabBufferCache.Release(obj);
        }
    }

    #endregion

    #region PrefabDataBuffer

    private readonly CachePool<PrefabDataBuffer> m_PrefabDataBufferCache = new CachePool<PrefabDataBuffer>();

    public PrefabDataBuffer RequirePrefabDataBuffer()
    {
        lock (m_PrefabDataBufferCache)
        {
            PrefabDataBuffer buffer = m_PrefabDataBufferCache.Get();
            return buffer;
        }
    }

    public void Collect(PrefabDataBuffer obj)
    {
        lock (m_PrefabDataBufferCache)
        {
            m_PrefabDataBufferCache.Release(obj);
        }
    }

    #endregion

    #region PrefabDataBufferList

    private readonly CachePool<PrefabDataBufferList> m_PrefabDataBufferListCache = new CachePool<PrefabDataBufferList>();

    public PrefabDataBufferList RequirePrefabDataBufferList()
    {
        lock (m_PrefabDataBufferListCache)
        {
            PrefabDataBufferList buffer = m_PrefabDataBufferListCache.Get();
            return buffer;
        }
    }

    public void Collect(PrefabDataBufferList obj)
    {
        var list = obj.list;
        if (list.Count > 0)
        {
            // 回收数据
            for (int i = 0; i < list.Count; ++i)
                Collect(list[i]);
        }

        // 清空引用
        obj.Clear();

        lock (m_PrefabDataBufferListCache)
        {
            m_PrefabDataBufferListCache.Release(obj);
        }
    }

    #endregion
}
