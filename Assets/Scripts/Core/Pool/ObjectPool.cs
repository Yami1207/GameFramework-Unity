using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;

public interface IObjectPool
{
    /// <summary>
    /// 所有已分配的个数
    /// </summary>
    int CountAll { get; }

    /// <summary>
    /// 所有正在使用的个数
    /// </summary>
    int CountActive { get; }

    /// <summary>
    /// 缓存中可用的列表
    /// </summary>
    int CountInactive { get; }
}

/// <summary>
/// 对象池
/// T必需为引用类型
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> : IObjectPool where T : class, new()
{
    // 这里用struct封T实现是减少GC检查对像的压力
    //[System.Diagnostics.DebuggerDisplay("{Value,nq}")]
    private struct Element
    {
        internal T value;
    }

    /// <summary>
    /// 第一个频繁存取的变量
    /// </summary>
    private T m_First;

    /// <summary>
    /// 所有列表
    /// </summary>
    private readonly Stack<Element> m_Stack = new Stack<Element>();

    /// <summary>
    /// 存取前操作
    /// </summary>
    private readonly UnityAction<T> m_GetAction;
    private readonly UnityAction<T> m_ReleaseAction;

    /// <summary>
    /// 所有已分配的个数
    /// </summary>
    public int CountAll { get; private set; }

    /// <summary>
    /// 所有正在使用的个数
    /// </summary>
    public int CountActive { get { return CountAll - CountInactive; } }

    /// <summary>
    /// 缓存中可用的列表
    /// </summary>
    public int CountInactive { get { return m_Stack.Count + ((m_First != null) ? 1 : 0); } }

    public ObjectPool(UnityAction<T> getAction, UnityAction<T> releaseAction)
    {
        m_GetAction = getAction;
        m_ReleaseAction = releaseAction;
    }

    public T Get()
    {
        T result = m_First;
        if (result != null)
        {
            m_First = null;
        }
        else
        {
            if (m_Stack.Count == 0)
            {
                result = new T();
                CountAll++;
            }
            else
            {
                result = m_Stack.Pop().value;
            }
        }

        if (m_GetAction != null)
            m_GetAction(result);
        return result;
    }

    public void Release(T obj)
    {
        UnityEngine.Debug.Assert(obj != null);
        if (m_ReleaseAction != null)
            m_ReleaseAction(obj);

        if (m_First == null)
            m_First = obj;
        else
            m_Stack.Push(new Element { value = obj });
    }
}
