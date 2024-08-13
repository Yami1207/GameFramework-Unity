using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static ReflectionPlane;

public class ReflectionRendererFeature : ScriptableRendererFeature
{
    public static readonly int REFLECTION_TEX_PROP_ID = Shader.PropertyToID("_G_ReflectionTex");

    public enum ReflectionType
    {
        None = 0,
        PlanarReflection,
        ScreenSpacePlanarReflection,
    }

    [SerializeField]
    private ReflectionType m_ReflectionType = ReflectionType.None;

    [SerializeField]
    private ReflectionQuality m_Quality = ReflectionQuality.High;
    public ReflectionQuality quality { get { return m_Quality; } }

    [SerializeField]
    private LayerMask m_CullingMask = -1;
    public LayerMask cullingMask { get { return m_CullingMask; } }

    [SerializeField]
    private bool m_RenderSkybox = false;
    public bool renderSkybox { get { return m_RenderSkybox; } }

    [SerializeField]
    private bool m_UseDoubleMapping = false;
    public bool useDoubleMapping { get { return m_UseDoubleMapping; } }

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_FadeOutToEdge = 0.3f;
    public float fadeOutToEdge { get { return m_FadeOutToEdge; } }

    [SerializeField]
    private bool m_FillHoles = true;
    public bool fillHoles { get { return m_FillHoles; } }

    private PlanarReflectionPass m_PlanarReflectionPass = null;
    private PlanarReflectionPass planarReflectionPass
    {
        get
        {
            if (m_PlanarReflectionPass == null)
                m_PlanarReflectionPass = new PlanarReflectionPass(this);
            return m_PlanarReflectionPass;
        }
    }

    private ScreenSpacePlanarReflectionPass m_ScreenSpacePlanarReflectionPass = null;
    private ScreenSpacePlanarReflectionPass screenSpacePlanarReflectionPass
    {
        get
        {
            if (m_ScreenSpacePlanarReflectionPass == null)
                m_ScreenSpacePlanarReflectionPass = new ScreenSpacePlanarReflectionPass(this);
            return m_ScreenSpacePlanarReflectionPass;
        }
    }

    public override void Create()
    {
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        if (ReflectionManager.instance.planes.Count == 0)
            return;

        if (m_ReflectionType == ReflectionType.PlanarReflection)
            renderer.EnqueuePass(planarReflectionPass);
        else if (m_ReflectionType == ReflectionType.ScreenSpacePlanarReflection && screenSpacePlanarReflectionPass.isVaild)
            renderer.EnqueuePass(screenSpacePlanarReflectionPass);
    }
}
