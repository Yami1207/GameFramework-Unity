using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Unity.VisualScripting.Member;

public class HiZDepth : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string s_ProfileTag = "HiZ Depth";

        private readonly HiZDepth m_Owner;

        public CustomRenderPass(HiZDepth owner)
        {
            m_Owner = owner;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(s_ProfileTag);
                {
                    HiZCore.instance.ExecuteCopyDepth(cmd, m_Owner.m_Quality);
                }
                cmd.EndSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }
    }

    [SerializeField]
    private HiZCore.DepthQuality m_Quality = HiZCore.DepthQuality.High;

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera == CameraManager.mainCamera)
            renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        HiZCore.instance.Destroy();
    }
}
