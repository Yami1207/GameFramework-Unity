using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelDepthOffset : ScriptableRendererFeature
{
    public enum TexQuality
    {
        High = 0,
        Middle,
        Low,
    }

    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string s_ProfileTag = "PDO";

        private static readonly int s_PDOAlbedoTexPropID = Shader.PropertyToID("_G_PDO_AlbedoTex");
        private static readonly int s_PDONormalTexPropID = Shader.PropertyToID("_G_PDO_NormalTex");

        private readonly PixelDepthOffset m_Owner;

        private readonly List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private FilteringSettings m_FilteringSettings;

        private RenderTargetIdentifier[] m_MultipleRenderTargets = new RenderTargetIdentifier[2];

        public CustomRenderPass(PixelDepthOffset owner)
        {
            m_Owner = owner;
            renderPassEvent = RenderPassEvent.BeforeRendering;

            m_ShaderTagIdList.Add(new ShaderTagId("PDO"));

            m_FilteringSettings = new FilteringSettings();
            m_FilteringSettings.layerMask = -1;
            m_FilteringSettings.renderingLayerMask = 0xffffffff;
            m_FilteringSettings.sortingLayerRange = SortingLayerRange.all;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int width = cameraTextureDescriptor.width >> (int)m_Owner.m_TexQuality;
            int height = cameraTextureDescriptor.height >> (int)m_Owner.m_TexQuality;

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            desc.width = width;
            desc.height = height;
            cmd.GetTemporaryRT(s_PDOAlbedoTexPropID, desc, FilterMode.Bilinear);

            RenderTextureDescriptor normalDesc = cameraTextureDescriptor;
            normalDesc.colorFormat = RenderTextureFormat.ARGB32;
            normalDesc.width = width;
            normalDesc.height = height;
            normalDesc.useMipMap = false;
            normalDesc.depthBufferBits = 0;
            normalDesc.msaaSamples = 1;
            normalDesc.sRGB = false;
            cmd.GetTemporaryRT(s_PDONormalTexPropID, normalDesc);

            m_MultipleRenderTargets[0] = s_PDOAlbedoTexPropID;
            m_MultipleRenderTargets[1] = s_PDONormalTexPropID;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref cameraData.renderer;
            Camera camera = cameraData.camera;
            RTHandle source = renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);

                // 清除旧数据
                cmd.Clear();
                Color cearColor = new Color(0.0f, 0.0f, 0.0f, camera.farClipPlane);
                CoreUtils.SetRenderTarget(cmd, m_MultipleRenderTargets, s_PDOAlbedoTexPropID, ClearFlag.All, cearColor);
                context.ExecuteCommandBuffer(cmd);

                // 设置渲染Layer
                m_FilteringSettings.layerMask = m_Owner.m_CullMask;

                // 只渲染不透明物
                m_FilteringSettings.renderQueueRange = RenderQueueRange.opaque;
                DrawingSettings drawingOpaqueSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawingOpaqueSettings, ref m_FilteringSettings);

                // 恢复FrameBuffer
                cmd.Clear();
                CoreUtils.SetRenderTarget(cmd, source);
                cmd.EndSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_PDOAlbedoTexPropID);
            cmd.ReleaseTemporaryRT(s_PDONormalTexPropID);
        }
    }

    [SerializeField]
    private TexQuality m_TexQuality = TexQuality.Low;

    [SerializeField]
    private LayerMask m_CullMask = -1;

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (EnvironmentCore.instance.enablePixelDepthOffset)
            renderer.EnqueuePass(m_ScriptablePass);
    }
}
