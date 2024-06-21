using System.IO;
using System.Collections;
using System.Collections.Generic;

public static class FilePath
{
    private static Dictionary<int, bool> s_FileExistsCache = new Dictionary<int, bool>();

    /// <summary>
    /// 快取路徑是否存在，暫時性解決 5.1.2p1 SD Card IO 卡的問題。
    /// </summary>
    public static bool Exists(string path)
    {
        int pathHash = path.GetHashCode();
        bool isExists;
        if (s_FileExistsCache.ContainsKey(pathHash))
        {
            isExists = s_FileExistsCache[pathHash];
        }
        else
        {
            isExists = File.Exists(path);
            s_FileExistsCache.Add(pathHash, isExists);
        }
        return isExists;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public static void Clear()
    {
        s_FileExistsCache.Clear();
    }
}
