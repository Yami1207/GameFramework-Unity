using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabNode : ObjectNode
{
    private GameObject m_Instance;

    public void Load(PrefabInfo info)
    {
        AssetManager.instance.LoadAssetAsync(info.assertID, OnCreateObject);
    }

    public void Clear()
    {
        if (m_Instance != null)
        {
            AssetManager.instance.RecycleGameObject(m_Instance);
            m_Instance = null;
        }
    }

    private void OnCreateObject(UnityEngine.Object obj)
    {
        if (obj != null && node != null)
        {
            m_Instance = AssetManager.instance.Instantiate(obj as GameObject);
            m_Instance.transform.SetParent(node.transform, false);
        }
    }

    #region ObjectNode Override

    /// <summary>
    /// 给子类重写，创建GameObject时调用
    /// </summary>
    protected override void OnCreate()
    {
        base.OnCreate();
    }

    /// <summary>
    /// 给子类重写，删除GameObject前时调用
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        Clear();
    }

    #endregion
}
