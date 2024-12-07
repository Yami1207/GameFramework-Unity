using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    internal class OutlinePass : ScriptableRendererFeature
    {
        private class CustomRenderPass : ScriptableRenderPass
        {
            private static readonly string s_ProfileTag = "Outline";

            private readonly OutlinePass m_Owner;

            private readonly List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

            private FilteringSettings m_FilteringSettings;

            public CustomRenderPass(OutlinePass owner)
            {
                m_Owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;

                m_ShaderTagIdList.Add(new ShaderTagId("Outline"));

                m_FilteringSettings = new FilteringSettings();
                m_FilteringSettings.layerMask = -1;
                m_FilteringSettings.renderingLayerMask = 0xffffffff;
                m_FilteringSettings.sortingLayerRange = SortingLayerRange.all;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                {
                    cmd.Clear();
                    cmd.BeginSample(s_ProfileTag);

                    // 只渲染不透明物
                    m_FilteringSettings.renderQueueRange = RenderQueueRange.opaque;
                    DrawingSettings drawingOpaqueSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                    context.DrawRenderers(renderingData.cullResults, ref drawingOpaqueSettings, ref m_FilteringSettings);

                    // 恢复FrameBuffer
                    cmd.Clear();
                    cmd.EndSample(s_ProfileTag);
                    context.ExecuteCommandBuffer(cmd);
                }
                CommandBufferPool.Release(cmd);
            }
        }

        private CustomRenderPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
