using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraOpaqueTexture : ScriptableRendererFeature
{
    public enum TexQuality
    {
        High = 0,
        Middle,
        Low,
    }

    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string PROFILE_TAG = "Screen Opaque Texture";

        private static readonly int SCREEN_OPAQUE_TEXTURE = Shader.PropertyToID("_G_ScreenOpaqueTexture");

        private readonly CameraOpaqueTexture m_Owner;

        public CustomRenderPass(CameraOpaqueTexture owner)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            m_Owner = owner;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int width = cameraTextureDescriptor.width >> (int)m_Owner.m_TexQuality;
            int height = cameraTextureDescriptor.height >> (int)m_Owner.m_TexQuality;

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.width = width;
            desc.height = height;
            desc.useMipMap = false;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            cmd.GetTemporaryRT(SCREEN_OPAQUE_TEXTURE, desc, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(PROFILE_TAG);
                cmd.Blit(source.nameID, SCREEN_OPAQUE_TEXTURE);
                cmd.EndSample(PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(SCREEN_OPAQUE_TEXTURE);
        }
    }

    [SerializeField]
    private TexQuality m_TexQuality = TexQuality.Low;

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
