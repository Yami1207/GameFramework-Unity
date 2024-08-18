using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ReflectionRendererFeature : ScriptableRendererFeature
{
    public static readonly int REFLECTION_TEX_PROP_ID = Shader.PropertyToID("_G_ReflectionTex");

    public enum ReflectionType
    {
        None = 0,
        PlanarReflection,
        ScreenSpaceReflection,
        ScreenSpacePlanarReflection,
    }

    [Serializable]
    public class PlanarReflectionSetting
    {
        public LayerMask cullingMask = -1;

        public bool renderSkybox = false;
    }

    [Serializable]
    public class ScreenSpaceReflectionSetting
    {
        public float thickness = 2.0f;

        public float stride = 0.3f;
    }

    [Serializable]
    public class ScreenSpacePlanarReflectionSetting
    {
        public bool useDoubleMapping = true;

        [Range(0.0f, 1.0f)]
        public float fadeOutToEdge = 0.3f;

        public bool fillHoles = true;
    }

    [SerializeField]
    private ReflectionType m_ReflectionType = ReflectionType.None;

    [SerializeField]
    private ReflectionQuality m_Quality = ReflectionQuality.High;
    public ReflectionQuality quality { get { return m_Quality; } }

    [SerializeField]
    private PlanarReflectionSetting m_PlanarReflectionSetting = new PlanarReflectionSetting();
    public PlanarReflectionSetting planarReflectionSetting { get { return m_PlanarReflectionSetting; } }

    [SerializeField]
    private ScreenSpaceReflectionSetting m_ScreenSpaceReflectionSetting = new ScreenSpaceReflectionSetting();
    public ScreenSpaceReflectionSetting SSRSetting { get { return m_ScreenSpaceReflectionSetting; } }

    [SerializeField]
    private ScreenSpacePlanarReflectionSetting m_ScreenSpacePlanarReflectionSetting = new ScreenSpacePlanarReflectionSetting();
    public ScreenSpacePlanarReflectionSetting SSPRSetting { get { return m_ScreenSpacePlanarReflectionSetting; } }

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

    private ScreenSpaceReflectionPass m_ScreenSpaceReflectionPass = null;
    private ScreenSpaceReflectionPass screenSpaceReflectionPass
    {
        get
        {
            if (m_ScreenSpaceReflectionPass == null)
                m_ScreenSpaceReflectionPass = new ScreenSpaceReflectionPass(this);
            return m_ScreenSpaceReflectionPass;
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
        if (ReflectionManager.instance.planes.Count == 0 || m_ReflectionType == ReflectionType.None)
        {
            Shader.SetGlobalTexture(REFLECTION_TEX_PROP_ID, Texture2D.blackTexture);
        }
        else
        {
            if (m_ReflectionType == ReflectionType.PlanarReflection)
                renderer.EnqueuePass(planarReflectionPass);
            else if (m_ReflectionType == ReflectionType.ScreenSpaceReflection)
                renderer.EnqueuePass(screenSpaceReflectionPass);
            else if (m_ReflectionType == ReflectionType.ScreenSpacePlanarReflection && screenSpacePlanarReflectionPass.isVaild)
                renderer.EnqueuePass(screenSpacePlanarReflectionPass);
        }
    }

    //protected override void Dispose(bool disposing)
    //{
    //    base.Dispose(disposing);
    //}
}
