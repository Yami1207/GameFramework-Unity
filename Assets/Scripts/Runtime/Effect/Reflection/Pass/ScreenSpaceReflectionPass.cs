using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        public static readonly int THICKNESS_PROP_ID = Shader.PropertyToID("_Thickness");

        public static readonly int STRIDE_PROP_ID = Shader.PropertyToID("_Stride");

        /// <summary>
        /// 裁剪空间变换矩阵
        /// </summary>
        public static readonly int VP_MATRIX_PROP_ID = Shader.PropertyToID("_VPMatrix");
    }

    private static readonly string PROFILE_TAG = "Screen Space Reflection";

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

    public delegate void DrawRenderer(CommandBuffer cmd, Material material, int shaderPass);
    public static DrawRenderer onDrawRenderer;

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

        SetupMaterial();

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

            var planes = m_Onwer.renderReflectionPlanes;
            for (int i = 0; i < planes.Count; ++i)
                cmd.DrawRenderer(planes[i].meshRenderer, material, 0, 0);

            if (onDrawRenderer != null)
                onDrawRenderer.Invoke(cmd, material, 0);

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

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.SSR_MASK_TEXTURE_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.SSR_DEPTH_TEXTURE_PROP_ID);
    }

    private void SetupMaterial()
    {
        var setting = m_Onwer.SSRSetting;

        var m = material;
        m.SetFloat(ShaderConstants.THICKNESS_PROP_ID, setting.thickness);
        m.SetFloat(ShaderConstants.STRIDE_PROP_ID, setting.stride);
    }
}
