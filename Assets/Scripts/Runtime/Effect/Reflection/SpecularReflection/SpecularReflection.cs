using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpecularReflection : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string s_ProfileTag = "Specular Reflection";

        private readonly List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private FilteringSettings m_FilteringSettings;

        public CustomRenderPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));

            m_FilteringSettings = new FilteringSettings();
            m_FilteringSettings.layerMask = -1;
            m_FilteringSettings.renderingLayerMask = 0xffffffff;
            m_FilteringSettings.sortingLayerRange = SortingLayerRange.all;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            {
                bool invertCulling = GL.invertCulling;
                ref UnityEngine.Rendering.Universal.CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                RenderTargetIdentifier depth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

                cmd.Clear();
                cmd.BeginSample(s_ProfileTag);
                cmd.SetInvertCulling(!invertCulling);
                context.ExecuteCommandBuffer(cmd);

                List<ReflectionPlane> planes = ReflectionManager.instance.planes;
                for (int i = 0; i < planes.Count; ++i)
                {
                    var reflectionPlane = planes[i];

                    cmd.Clear();
                    cmd.SetRenderTarget(reflectionPlane.texture);
                    cmd.ClearRenderTarget(true, true, Color.black);

                    // 反射平面
                    Vector3 planeNormal = reflectionPlane.transform.up;
                    Vector3 planePoint = reflectionPlane.transform.position;
                    Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector2.Dot(planeNormal, planePoint));

                    // 视角矩阵
                    Matrix4x4 reflectMatrix = CalculateReflectMatrix(plane);
                    Matrix4x4 worldToCameraMatrix = camera.worldToCameraMatrix * reflectMatrix;

                    // 投影矩阵
                    Matrix4x4 projectionMatrix = camera.projectionMatrix;
                    projectionMatrix = CalculateObliqueMatrix(plane, ref worldToCameraMatrix, ref projectionMatrix);

                    // 设置视角与投影矩阵
                    cmd.SetViewProjectionMatrices(worldToCameraMatrix, projectionMatrix);
                    context.ExecuteCommandBuffer(cmd);

                    // 过滤反射层
                    m_FilteringSettings.layerMask = reflectionPlane.cullMask;

                    // 渲染不透明物
                    m_FilteringSettings.renderQueueRange = RenderQueueRange.opaque;
                    DrawingSettings drawingOpaqueSettings = this.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonOpaque);
                    context.DrawRenderers(renderingData.cullResults, ref drawingOpaqueSettings, ref m_FilteringSettings);

                    // 天空盒
                    if (RenderSettings.skybox != null || (camera.TryGetComponent(out Skybox cameraSkybox) && cameraSkybox.material != null))
                        context.DrawSkybox(renderingData.cameraData.camera);

                    // 渲染透明物
                    m_FilteringSettings.renderQueueRange = RenderQueueRange.transparent;
                    DrawingSettings drawingTransparentSettings = this.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                    context.DrawRenderers(renderingData.cullResults, ref drawingTransparentSettings, ref m_FilteringSettings);
                }

                // 恢复FrameBuffer
                cmd.Clear();
                cmd.SetViewProjectionMatrices(cameraData.GetViewMatrix(), cameraData.GetProjectionMatrix());
                cmd.SetRenderTarget(source, depth);
                cmd.SetInvertCulling(invertCulling);
                cmd.EndSample(s_ProfileTag);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 计算反射矩阵
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="positionOnPlane"></param>
        /// <returns></returns>
        private static Matrix4x4 CalculateReflectMatrix(Vector4 plane)
        {
            Vector3 normal = new Vector3(plane.x, plane.y, plane.z);
            float d = plane.w;

            var reflectMatrix = Matrix4x4.identity;
            reflectMatrix.m00 = 1 - 2 * normal.x * normal.x;
            reflectMatrix.m01 = -2 * normal.x * normal.y;
            reflectMatrix.m02 = -2 * normal.x * normal.z;
            reflectMatrix.m03 = -2 * d * normal.x;

            reflectMatrix.m10 = -2 * normal.x * normal.y;
            reflectMatrix.m11 = 1 - 2 * normal.y * normal.y;
            reflectMatrix.m12 = -2 * normal.y * normal.z;
            reflectMatrix.m13 = -2 * d * normal.y;
            
            reflectMatrix.m20 = -2 * normal.x * normal.z;
            reflectMatrix.m21 = -2 * normal.y * normal.z;
            reflectMatrix.m22 = 1 - 2 * normal.z * normal.z;
            reflectMatrix.m23 = -2 * d * normal.z;

            return reflectMatrix;
        }

        /// <summary>
        /// 计算斜截视锥体
        /// https://blog.csdn.net/a1047120490/article/details/106743734/
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="worldToCameraMatrix"></param>
        /// <param name="projectionMatrix"></param>
        /// <returns></returns>
        private static Matrix4x4 CalculateObliqueMatrix(Vector4 plane, ref Matrix4x4 worldToCameraMatrix, ref Matrix4x4 projectionMatrix)
        {
            var M = projectionMatrix;
            // 因为是协变向量，需要逆转置来变换,
            // N'
            var newN = worldToCameraMatrix.inverse.transpose * plane;
            // Q'
            var qInClipSpace = new Vector4(Mathf.Sign(newN.x), Mathf.Sign(newN.y), 1, 1);
            var qInViewSpace = projectionMatrix.inverse * qInClipSpace;
            // 求a
            var a = 2f / Vector4.Dot(newN, qInViewSpace);
            var m4 = new Vector4(M.m30, M.m31, M.m32, M.m33);
            var a_newN = a * newN;
            // m'_3
            var newM_3 = a_newN - m4;
            M.m20 = newM_3.x;
            M.m21 = newM_3.y;
            M.m22 = newM_3.z;
            M.m23 = newM_3.w;
            return M;
        }
    }

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        if (ReflectionManager.instance.planes.Count > 0)
            renderer.EnqueuePass(m_ScriptablePass);
    }
}
