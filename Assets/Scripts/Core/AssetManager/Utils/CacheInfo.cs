using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CacheInfo
{
    /// <summary>
    /// 缓存资源
    /// </summary>
    private UnityEngine.Object m_Asset;
    public UnityEngine.Object asset { get { return m_Asset; } }

    /// <summary>
    /// 资源ID
    /// </summary>
    private int m_AssetId;
    public int assetId { get { return m_AssetId; } }

    /// <summary>
    /// 使用时间
    /// </summary>
    private float m_UseTime;
    public float useTime { get { return m_UseTime; } }

    public CacheInfo(UnityEngine.Object asset, int assetId = -1)
    {
        m_Asset = asset;
        m_AssetId = assetId;
        m_UseTime = Time.time;
    }

    /// <summary>
    /// 设置使用
    /// </summary>
    public void Use()
    {
        m_UseTime = Time.time;
    }
}
