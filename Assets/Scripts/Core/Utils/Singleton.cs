public class Singleton<T> where T : new()
{
    private static object s_ObjectLock = new object();
    private static T s_Instance;

    public static T instance
    {
        get
        {
            if (s_Instance == null)
            {
                lock (s_ObjectLock)
                {
                    if (s_Instance == null)
                        s_Instance = new T();
                }
            }
            return s_Instance;
        }
    }
}
