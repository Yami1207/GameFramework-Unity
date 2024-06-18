using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    /// <summary>
    /// 管理物件
    /// </summary>
    private GameObject m_PoolObject;

    /// <summary>
    /// 缓存GameObject、Prefab的映射关系
    /// </summary>
    private Dictionary<GameObject, GameObject> m_PrefabMap = new Dictionary<GameObject, GameObject>();

    /// <summary>
    /// 缓存GameObject资源
    /// </summary>
    private readonly Dictionary<string, Stack<GameObject>> m_CacheDict = new Dictionary<string, Stack<GameObject>>();

    /// <summary>
    /// 缓存列表对象
    /// </summary>
    private readonly Stack<Stack<GameObject>> m_StackPool = new Stack<Stack<GameObject>>();

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init()
    {
        if (m_PoolObject != null)
            return;

        // 创建对象并设置容量,防止频繁分配
        m_PoolObject = new GameObject("PoolManager");
        m_PoolObject.SetActive(false);
        m_PoolObject.transform.hierarchyCapacity = 4096;
    }

    /// <summary>
    /// 销毁
    /// </summary>
    public void Destroy()
    {
        m_CacheDict.Clear();
        m_PrefabMap.Clear();

        if (m_PoolObject != null)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEngine.Object.Destroy(m_PoolObject);
            else
                UnityEngine.Object.DestroyImmediate(m_PoolObject);
#else
            UnityEngine.Object.Destroy(m_PoolObject);
#endif

            m_PoolObject = null;
        }
    }

    /// <summary>
    /// 获取GameObject
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public GameObject Get(string key)
    {
        Stack<GameObject> stack;
        if (!m_CacheDict.TryGetValue(key, out stack) || stack.Count <= 0)
            return null;

        GameObject go = stack.Pop();
        if (stack.Count <= 0)
        {
            // 回收列表
            m_StackPool.Push(stack);
            m_CacheDict.Remove(key);
        }

        if (go != null)
            go.transform.SetParent(null);
        return go;
    }

    public void ReturnToPool(GameObject go)
    {
        if (go == null)
            return;

        if (m_PoolObject == null)
        {
            UnityEngine.Object.Destroy(go);
#if UNITY_EDITOR
            Debug.LogWarning("PoolManager 已经被销毁!");
#endif
            return;
        }

        // 已回收
        Transform t = go.transform;
        if (t.parent == m_PoolObject)
            return;

        // 回收
        t.SetParent(m_PoolObject.transform);

        string key = go.name;
        Stack<GameObject> stack;
        if (!m_CacheDict.TryGetValue(key, out stack))
        {
            if (m_StackPool.Count > 0)
            {
                stack = m_StackPool.Pop();
                Debug.Assert(stack.Count == 0);
            }
            else
            {
                stack = new Stack<GameObject>();
            }

            m_CacheDict.Add(key, stack);
            stack.Push(go);
        }
        else
        {
            if (stack.Contains(go))
            {
                throw new UnityException("重复回收GameObject:" + go.name);
            }
            else
            {
                if (stack.Count > 0)
                {
                    GameObject lastPrefab = m_PrefabMap[stack.Peek()];
                    GameObject nowPrefab = m_PrefabMap[go];

                    if (lastPrefab != nowPrefab)
                        throw new UnityException(string.Format("对象缓存池出错！不同的Prefab有相同的名字，prefabName={0}", key));
                }
                stack.Push(go);
            }
        }
    }

    /// <summary>
    /// 添加物体映射表
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="prefab"></param>
    public void AddToPrefabMap(GameObject gameObject, GameObject prefab)
    {
        m_PrefabMap[gameObject] = prefab;
    }

    /// <summary>
    /// 移除物体映射表
    /// </summary>
    /// <param name="gameObject"></param>
    public void RemoveFromPrefabMap(GameObject gameObject)
    {
        if (m_PrefabMap.ContainsKey(gameObject))
            m_PrefabMap.Remove(gameObject);
    }
}
