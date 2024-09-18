using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancingPrefab
{
    private InstancingCore m_InstancingCore;

    private InstancingRenderer m_Renderer;

    private List<Matrix4x4> m_InstanceList;

    public int visibleDistance { get { return m_Renderer == null ? -1 : m_Renderer.visibleDistance; } }

    public void Init(InstancingCore core)
    {
        m_InstancingCore = core;
        m_InstanceList = m_InstancingCore.factory.CreateList_Matrix4x4();
    }

    public void Clear()
    {
        if (m_InstanceList != null)
        {
            m_InstancingCore.factory.Collect(m_InstanceList);
            m_InstanceList = null;
        }

        m_Renderer = null;
        m_InstancingCore = null;
    }

    public void Load(int id, PrefabInfo info)
    {
        Debug.Assert(m_InstancingCore != null);
        Debug.Assert(m_Renderer == null);
        m_Renderer = m_InstancingCore.GetPrefabRenderer(info.assertID);
    }

    public void Perform()
    {
        Debug.Assert(m_Renderer != null);
        m_Renderer.RequireInstances(m_InstanceList);
    }

    public void AddInstance(Matrix4x4 transform)
    {
        m_InstanceList.Add(transform);
    }
}
