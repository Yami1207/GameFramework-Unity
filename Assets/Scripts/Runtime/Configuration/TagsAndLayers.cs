using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TagsAndLayers
{
    #region Layers

    public static readonly int DEFAULT_LAYER = LayerMask.NameToLayer("Default");

    /// <summary>
    /// 水
    /// </summary>
    public static readonly int WATER_LAYER = LayerMask.NameToLayer("Water");

    /// <summary>
    /// 地形
    /// </summary>
    public static readonly int TERRAIN_LAYER = LayerMask.NameToLayer("Terrain");

    #endregion
}
