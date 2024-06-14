using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;

public class VolumetricCloud : ScriptableRendererFeature
{
    private static class ShaderConstants
    {
        public static readonly int kVolumetricTexture = Shader.PropertyToID("g_VolumetricCloudTex");

        public static readonly int kHeightPropID = Shader.PropertyToID("_Height");
        public static readonly int kThicknessPropID = Shader.PropertyToID("_Thickness");
        public static readonly int kStepCountPropID = Shader.PropertyToID("_StepCount");
    }

    private class CustomRenderPass : ScriptableRenderPass
    {
        //private static readonly string kProfileTag = "Volumetric Cloud";

        private readonly VolumetricCloud m_Owner;

        private RenderTargetIdentifier m_VolumetricTextureID = new RenderTargetIdentifier(ShaderConstants.kVolumetricTexture);

        public CustomRenderPass(VolumetricCloud owner)
        {
            m_Owner = owner;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(ShaderConstants.kVolumetricTexture, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //var setting = EnvironmentSetting.instance.cloud;
            //if (!setting.enabled)
            //    return;

            //CommandBuffer cmd = CommandBufferPool.Get();
            //{
            //    cmd.Clear();
            //    cmd.BeginSample(kProfileTag);
            //    context.ExecuteCommandBuffer(cmd);

            //    RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            //    cmd.Clear();
            //    cmd.Blit(source, m_VolumetricTextureID, m_Owner.m_Material);
            //    cmd.Blit(m_VolumetricTextureID, source);
            //    context.ExecuteCommandBuffer(cmd);

            //    cmd.Clear();
            //    cmd.EndSample(kProfileTag);
            //    context.ExecuteCommandBuffer(cmd);
            //}
            //CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(ShaderConstants.kVolumetricTexture);
        }
    }

    [SerializeField]
    private Material m_Material;

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //if (m_Material == null)
        //    return;

        //var setting = EnvironmentSetting.instance.cloud;
        //if (!setting.enabled)
        //    return;

        //SetupMaterial(setting);
        //renderer.EnqueuePass(m_ScriptablePass);
    }

    private void SetupMaterial(CloudSetting setting)
    {
        //m_Material.SetFloat(ShaderConstants.kHeightPropID, setting.height);
        //m_Material.SetFloat(ShaderConstants.kThicknessPropID, setting.thickness);
        //m_Material.SetFloat(ShaderConstants.kStepCountPropID, setting.setpCount);
    }
}
