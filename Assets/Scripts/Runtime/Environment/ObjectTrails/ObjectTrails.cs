using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class ObjectTrails : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string s_ProfileTag = "Object Trails";

        private static readonly int s_ObjectTrailsTexPropID = Shader.PropertyToID("_G_ObjectTrailsTex");

        private static readonly int s_ObjectTrailsTexPosPropID = Shader.PropertyToID("_G_ObjectTrailsTexPos");
        private static readonly int s_ObjectTrailsTexHeightPropID = Shader.PropertyToID("_G_ObjectTrailsTexHeight");

        private readonly ObjectTrails m_Owner;

        private readonly List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private FilteringSettings m_FilteringSettings;

        public CustomRenderPass(ObjectTrails owner)
        {
            m_Owner = owner;
            renderPassEvent = RenderPassEvent.BeforeRendering;

            m_ShaderTagIdList.Add(new ShaderTagId("ObjectTrails"));

            m_FilteringSettings = new FilteringSettings();
            m_FilteringSettings.layerMask = -1;
            m_FilteringSettings.renderingLayerMask = 0xffffffff;
            m_FilteringSettings.sortingLayerRange = SortingLayerRange.all;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var config = EnvironmentCore.instance.objectTrails;
            int size = (int)config.resolution;

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            desc.width = size;
            desc.height = size;
            desc.depthBufferBits = 0;
            desc.sRGB = false;
            cmd.GetTemporaryRT(s_ObjectTrailsTexPropID, desc, FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var config = EnvironmentCore.instance.objectTrails;
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            RenderTargetIdentifier source = cameraData.renderer.cameraColorTargetHandle;
            RenderTargetIdentifier depth = cameraData.renderer.cameraDepthTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);

                // 清除旧数据
                cmd.Clear();
                cmd.SetRenderTarget(s_ObjectTrailsTexPropID);
                cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                context.ExecuteCommandBuffer(cmd);

                // 视角矩阵
                Vector3 position = camera.transform.position + new Vector3(0.0f, config.cameraHeight, 0.0f);
                Matrix4x4 worldToCameraMatrix = Matrix4x4.LookAt(position, position + Vector3.down, Vector3.forward);
                worldToCameraMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1.0f, 1.0f, -1.0f)) * worldToCameraMatrix.inverse;

                // 投影矩阵
                float halfRange = 0.5f * config.cameraRange;
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-halfRange, halfRange, -halfRange, halfRange, config.cameraNear, config.cameraFar);
                projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);

                // 设置视角与投影矩阵
                cmd.Clear();
                cmd.SetViewProjectionMatrices(worldToCameraMatrix, projectionMatrix);
                cmd.SetGlobalVector(s_ObjectTrailsTexPosPropID, new Vector4(position.x - halfRange, position.z - halfRange, config.cameraRange));
                cmd.SetGlobalVector(s_ObjectTrailsTexHeightPropID, new Vector4(position.y - (config.cameraFar - config.cameraNear), position.y, 0.0f));
                context.ExecuteCommandBuffer(cmd);

                // 只渲染不透明物
                m_FilteringSettings.renderQueueRange = RenderQueueRange.opaque;
                DrawingSettings drawingOpaqueSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                context.DrawRenderers(renderingData.cullResults, ref drawingOpaqueSettings, ref m_FilteringSettings);

                // 恢复FrameBuffer
                cmd.Clear();
                cmd.SetViewProjectionMatrices(cameraData.GetViewMatrix(), cameraData.GetProjectionMatrix());
                cmd.SetRenderTarget(source, depth);
                cmd.EndSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_ObjectTrailsTexPropID);
        }
    }

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var config = EnvironmentCore.instance.objectTrails;
        if (config != null)
            renderer.EnqueuePass(m_ScriptablePass);
    }
}
