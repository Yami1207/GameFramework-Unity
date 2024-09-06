using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabInfo
{
    private int m_AssetID;
    public int assertID { get { return m_AssetID; } }

    private bool m_UseInstancing;
    public bool useInstancing { get { return m_UseInstancing; } }

    private int m_VisibleDistance;
    public int visibleDistance { get { return m_VisibleDistance; } }

    private bool m_FrustumCulling;
    public bool frustumCulling { get { return m_FrustumCulling; } }

    private bool m_OcclusionCulling;
    public bool occlusionCulling { get { return m_OcclusionCulling; } }

    private int m_Layer = 0;
    public int layer { get { return m_Layer; } }

    #region 静态方法

    private static Dictionary<int, PrefabInfo> s_PrefabInfoDict = new Dictionary<int, PrefabInfo>();

    public static bool isInitialized
    {
        get { return s_PrefabInfoDict.Count > 0; }
    }

    public static void Load()
    {
        if (isInitialized)
            return;

        CSVPrefabNode.Load();

        var iter = CSVPrefabNode.GetAllDict().GetEnumerator();
        while (iter.MoveNext())
        {
            CSVPrefabNode node = iter.Current.Value;
            PrefabInfo info = new PrefabInfo();
            info.m_AssetID = node.asset_id;
            info.m_UseInstancing = node.instancing;
            info.m_VisibleDistance = node.visible;
            info.m_FrustumCulling = node.enable_frustum_culling;
            info.m_OcclusionCulling = node.enable_occlusion_culling;
            int layer = LayerMask.NameToLayer(node.layer);
            info.m_Layer = layer == -1 ? 0 : layer;
            s_PrefabInfoDict.Add(node.id, info);
        }
        iter.Dispose();

        CSVPrefabNode.Unload();
    }

    public static void Clear()
    {
        s_PrefabInfoDict.Clear();
    }

    public static PrefabInfo Get(int id)
    {
#if UNITY_EDITOR
        if (s_PrefabInfoDict.Count == 0)
        {
            Debug.LogError("资源表为空！请先初始化资源表！");
            return null;
        }
#endif

        PrefabInfo info = null;
        s_PrefabInfoDict.TryGetValue(id, out info);
        return info;
    }

    #endregion
}
