public static class Define
{
    #region Prefab

    public static readonly string kUIRootPrefab = "Prefab/UI/UIRoot";

    #endregion

    // Chunk边长
    public static readonly int kChunkSideLengthBits = 4;
    public static readonly int kChunkSideLength = 16;
    public static readonly int kChunkSideLengthMinusOne = kChunkSideLength - 1;

    // Scene边长
    public static readonly int kSceneSideLength = 1024 >> kChunkSideLengthBits;
    public static readonly int kSceneSideLengthMinusOne = kSceneSideLength - 1;

    /// <summary>
    /// Chunk加载距离
    /// </summary>
    public static readonly int kChunkLoadDistance = 14;

    /// <summary>
    /// Chunk卸载距离
    /// </summary>
    public static readonly int kChunkUnloadDistance = 18;

    /// <summary>
    /// 卸载Chunk数据的存活时间
    /// </summary>
    public static readonly long kChunkAliveTime = 5000;
}
