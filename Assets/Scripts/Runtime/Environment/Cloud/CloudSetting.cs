using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CloudSetting
{
    public bool enabled;

    public Color color;

    /// <summary>
    /// 云层高度
    /// </summary>
    public float height;

    /// <summary>
    /// 云层厚度
    /// </summary>
    public float thickness;

    /// <summary>
    /// 光线步进次数
    /// </summary>
    public int setpCount;
}
