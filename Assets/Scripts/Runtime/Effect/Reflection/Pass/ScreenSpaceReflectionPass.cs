using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;

public class ScreenSpaceReflectionPass : BaseReflectionPass
{
    private static class ShaderConstants
    {
        public static readonly int SSR_MASK_TEXTURE_PROP_ID = Shader.PropertyToID("_SSRMaskTexture");

        public static readonly int SSR_DEPTH_TEXTURE_PROP_ID = Shader.PropertyToID("_SSRDepthTexture");

        /// <summary>
        /// 当前颜色图
        /// </summary>
        public static readonly int CAMERA_COLOR_TEXTURE_PROP_ID = Shader.PropertyToID("_CameraColorTexture");

        /// <summary>
        /// 裁剪空间变换矩阵
        /// </summary>
        public static readonly int VP_MATRIX_PROP_ID = Shader.PropertyToID("_VPMatrix");
    }

    private static readonly string PROFILE_TAG = "Screen Space Reflection";

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
    private List<MeshRenderer> m_RenderReflectionPlanes = new List<MeshRenderer>(8);

    private Material m_ReflectionMaterial;
    public Material material
    {
        get
        {
            if (m_ReflectionMaterial == null)
                m_ReflectionMaterial = new Material(Shader.Find("Hidden/Reflection/ScreenSpaceReflection"));
            return m_ReflectionMaterial;
        }
    }

    public ScreenSpaceReflectionPass(ReflectionRendererFeature owner) : base(owner)
    {
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        Vector2Int size = GetTextureSize(m_Onwer.quality, cameraTextureDescriptor.width, cameraTextureDescriptor.height);
        RenderTextureDescriptor desc = cameraTextureDescriptor;
        desc.colorFormat = RenderTextureFormat.ARGB32;
        desc.width = size.x;
        desc.height = size.y;
        desc.autoGenerateMips = false;
        desc.useMipMap = false;
        desc.depthBufferBits = 0;
        cmd.GetTemporaryRT(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID, desc, FilterMode.Bilinear);

        desc.sRGB = false;
        cmd.GetTemporaryRT(ShaderConstants.SSR_MASK_TEXTURE_PROP_ID, desc, FilterMode.Bilinear);

        desc.depthBufferBits = 24;
        desc.colorFormat = RenderTextureFormat.Depth;
        cmd.GetTemporaryRT(ShaderConstants.SSR_DEPTH_TEXTURE_PROP_ID, desc);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        ref CameraData cameraData = ref renderingData.cameraData;
        ref ScriptableRenderer renderer = ref cameraData.renderer;
        Camera camera = cameraData.camera;
#if UNITY_2022_1_OR_NEWER
        RTHandle colorTarget = renderer.cameraColorTargetHandle;
        RTHandle depthTarget = renderer.cameraDepthTargetHandle;
#else
        RenderTargetIdentifier colorTarget = renderer.cameraColorTarget;
        RenderTargetIdentifier depthTarget = renderer.cameraDepthTarget;
#endif

        SetupFrustumPlanes(camera);
        SetupReflectionPlanes();

        if (m_RenderReflectionPlanes.Count > 0)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();

                // 渲染反射平面信息
                cmd.Clear();
                CoreUtils.SetRenderTarget(cmd, ShaderConstants.SSR_MASK_TEXTURE_PROP_ID, ShaderConstants.SSR_DEPTH_TEXTURE_PROP_ID);
                cmd.ClearRenderTarget(true, true, m_ClearColor);
                for (int i = 0; i < m_RenderReflectionPlanes.Count; ++i)
                    cmd.DrawRenderer(m_RenderReflectionPlanes[i], material, 0, 0);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                CoreUtils.SetRenderTarget(cmd, ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);

                // 裁剪空间变换矩阵
                Matrix4x4 viewProjectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
                cmd.SetGlobalMatrix(ShaderConstants.VP_MATRIX_PROP_ID, viewProjectionMatrix);

                //cmd.SetRenderTarget(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);
                cmd.Blit(ShaderConstants.SSR_MASK_TEXTURE_PROP_ID, ReflectionRendererFeature.REFLECTION_TEX_PROP_ID, material, 1);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                cmd.SetRenderTarget(colorTarget, depthTarget);
                cmd.EndSample(PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.SSR_MASK_TEXTURE_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.SSR_DEPTH_TEXTURE_PROP_ID);
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
            var renderer = planes[i].meshRenderer;
            Bounds planesBounds = renderer.bounds;
            if (Utils.TestPlanesAABB(ref m_CameraFrustumPlanes, planesBounds.min, planesBounds.max))
                m_RenderReflectionPlanes.Add(renderer);
        }
    }
}
