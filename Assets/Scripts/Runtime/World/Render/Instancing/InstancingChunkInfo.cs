using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InstancingChunkInfo
{
    public int x, z;

    /// <summary>
    /// 存储列表索引值
    /// </summary>
    public int index;

    /// <summary>
    /// 包围盒最小值
    /// </summary>
    public Vector4 minBounds;

    /// <summary>
    /// 包围盒最大值
    /// </summary>
    public Vector4 maxBounds;

    private static InstancingChunkInfo s_Empty = new InstancingChunkInfo() { index = -1 };
    public static InstancingChunkInfo empty { get { return s_Empty; } }
}
