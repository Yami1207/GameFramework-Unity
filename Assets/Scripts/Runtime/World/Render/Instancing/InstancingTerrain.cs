using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.PlayerSettings;

public class InstancingTerrain
{
    private InstancingCore m_InstancingCore;

    private InstancingRenderer m_StandardRenderer;
    private InstancingRenderer m_StandardAddRenderer;
    private InstancingRenderer m_LowRenderer;

    private static Mesh s_TerrainMesh = null;

    public InstancingTerrain(InstancingCore core)
    {
        m_InstancingCore = core;
    }

    public void Clear()
    {
        if (m_StandardRenderer != null)
        {
            m_InstancingCore.RemoveRenderer(m_StandardRenderer);
            m_StandardRenderer = null;
        }
            
        if (m_StandardAddRenderer != null)
        {
            m_InstancingCore.RemoveRenderer(m_StandardAddRenderer);
            m_StandardAddRenderer = null;
        }

        if (m_LowRenderer != null)
        {
            m_InstancingCore.RemoveRenderer(m_LowRenderer);
            m_LowRenderer = null;
        }

        m_InstancingCore = null;
    }

    public void RenderChunk(InstancingChunk chunk)
    {
        Vector3 cameraPos = CameraManager.mainCamera.transform.position;
        int x = Mathf.FloorToInt(cameraPos.x) >> Define.kChunkSideLengthBits, z = Mathf.FloorToInt(cameraPos.z) >> Define.kChunkSideLengthBits;
        int dx = chunk.chunkPos.x - x, dz = chunk.chunkPos.z - z;
        if (dx * dx + dz * dz <= GameSetting.lowTerrainDistance * GameSetting.lowTerrainDistance)
        {
            m_StandardRenderer.RequireInstance(chunk.transform);

            if (chunk.hasExtend)
                m_StandardAddRenderer.RequireInstance(chunk.transform);
        }
        else
        {
            m_LowRenderer.RequireInstance(chunk.transform);
        }
    }

    public void SetMaterials(Material standard, Material standardAdd, Material low)
    {
        Debug.Assert(m_StandardRenderer == null);
        Debug.Assert(m_StandardAddRenderer == null);
        Debug.Assert(m_LowRenderer == null);

        if (s_TerrainMesh == null)
            s_TerrainMesh = AssetManager.instance.LoadAsset<Mesh>(2000);

        m_StandardRenderer = m_InstancingCore.CreateSingleRenderer();
        m_StandardRenderer.enableFrustumCulling = false;
        m_StandardRenderer.AddDrawcall(m_InstancingCore.CreateInstancingDrawcall(s_TerrainMesh, standard, ShadowCastingMode.Off, true));

        m_StandardAddRenderer = m_InstancingCore.CreateSingleRenderer();
        m_StandardAddRenderer.enableFrustumCulling = false;
        m_StandardAddRenderer.AddDrawcall(m_InstancingCore.CreateInstancingDrawcall(s_TerrainMesh, standardAdd, ShadowCastingMode.Off, true));

        m_LowRenderer = m_InstancingCore.CreateSingleRenderer();
        m_LowRenderer.enableFrustumCulling = false;
        m_LowRenderer.AddDrawcall(m_InstancingCore.CreateInstancingDrawcall(s_TerrainMesh, low, ShadowCastingMode.Off, true));
    }
}
