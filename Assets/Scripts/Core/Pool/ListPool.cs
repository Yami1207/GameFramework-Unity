using System.Collections.Generic;

public static class ListPool<T>
{
    private static readonly ObjectPool<List<T>> s_ListPool = new ObjectPool<List<T>>(null, l => l.Clear());

    public static List<T> Get()
    {
        return s_ListPool.Get();
    }

    public static List<T> Get(int size)
    {
        var list = s_ListPool.Get();
        if (list.Capacity < size)
        {
            if (size < 1024)
                size = UnityEngine.Mathf.NextPowerOfTwo(size);
            list.Capacity = size;
        }
        return list;
    }

    public static void Release(List<T> obj)
    {
        s_ListPool.Release(obj);
    }
}
