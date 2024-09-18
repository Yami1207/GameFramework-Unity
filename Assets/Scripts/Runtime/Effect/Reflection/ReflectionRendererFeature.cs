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

    /// <summary>
    /// 镜头视锥
    /// </summary>
    private Plane[] m_CameraFrustums = new Plane[6];

    /// <summary>
    /// 镜头视锥平面（xyz:normal w:distance）
    /// </summary>
    private Vector4[] m_CameraFrustumPlanes = new Vector4[6];

    /// <summary>
    /// 当前在视锥内的反射平面
    /// </summary>
    private List<ReflectionPlane> m_RenderReflectionPlanes = new List<ReflectionPlane>(8);
    public List<ReflectionPlane> renderReflectionPlanes { get { return m_RenderReflectionPlanes; } }

    /// <summary>
    /// 获取GPUDriven里的反射平面数量
    /// </summary>
    /// <returns></returns>
    public delegate bool HasInstancingReflectionPlane();
    public static HasInstancingReflectionPlane hasInstancingReflectionPlane = null;

    public delegate bool GetInstancingReflectionPlane(out Vector4 plane);
    public static GetInstancingReflectionPlane instancingReflectionPlane;

    public override void Create()
    {
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        SetupFrustumPlanes(renderingData.cameraData.camera);
        SetupReflectionPlanes();

        int count = m_RenderReflectionPlanes.Count;
        if (hasInstancingReflectionPlane != null && hasInstancingReflectionPlane.Invoke())
            ++count;

        if (count == 0 || m_ReflectionType == ReflectionType.None)
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

    private void SetupFrustumPlanes(Camera camera)
    {
        // 获取镜头视锥体
        GeometryUtility.CalculateFrustumPlanes(camera, m_CameraFrustums);
        for (int i = 0; i < 6; ++i)
        {
            var normal = m_CameraFrustums[i].normal;
            var d = m_CameraFrustums[i].distance;
            m_CameraFrustumPlanes[i].Set(normal.x, normal.y, normal.z, d);
        }
    }

    private void SetupReflectionPlanes()
    {
        m_RenderReflectionPlanes.Clear();

        List<ReflectionPlane> planes = ReflectionManager.instance.planes;
        for (int i = 0; i < planes.Count; ++i)
        {
            var plane = planes[i];
            var renderer = plane.meshRenderer;
            Bounds planesBounds = renderer.bounds;
            if (Utils.TestPlanesAABB(ref m_CameraFrustumPlanes, planesBounds.min, planesBounds.max))
                m_RenderReflectionPlanes.Add(plane);
        }
    }
}
