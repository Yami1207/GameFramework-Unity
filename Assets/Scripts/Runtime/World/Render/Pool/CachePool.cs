using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CachePool<T> where T : new()
{
    private struct Element
    {
        internal T value;
    }

    private readonly Stack<Element> m_Stack;

    /// <summary>
    /// 获得缓冲数量
    /// </summary>
    public int count { get { return m_Stack.Count; } }

    public CachePool()
    {
        m_Stack = new Stack<Element>();
    }

    public CachePool(int capacity)
    {
        m_Stack = new Stack<Element>(capacity);
    }

    /// <summary>
    /// 从缓冲获得对象
    /// </summary>
    /// <returns></returns>
    public T Get()
    {
        T element;
        if (m_Stack.Count == 0)
            element = new T();
        else
            element = m_Stack.Pop().value;
        return element;
    }

    /// <summary>
    /// 把对象放入缓存池中
    /// </summary>
    /// <param name="element"></param>
    public void Release(T element)
    {
#if UNITY_EDITOR
        if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
            Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
#endif
        m_Stack.Push(new Element { value = element });
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        m_Stack.Clear();
    }
}
