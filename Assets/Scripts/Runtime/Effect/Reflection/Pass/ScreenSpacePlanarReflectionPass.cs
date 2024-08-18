using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpacePlanarReflectionPass : BaseReflectionPass
{
    private static class ShaderConstants
    {
        /// <summary>
        /// 边缘淡出
        /// </summary>
        public static readonly int FADE_OUT_TO_EDGE_PROP_ID = Shader.PropertyToID("_FadeOutToEdge");

        /// <summary>
        /// 反射平面
        /// </summary>
        public static readonly int REFLECTION_PLANE_PROP_ID = Shader.PropertyToID("_ReflectionPlane");

        /// <summary>
        /// 纹理大小
        /// </summary>
        public static readonly int COLOR_TEXTURE_SIZE_PROP_ID = Shader.PropertyToID("_ColorTextureSize");

        /// <summary>
        /// 裁剪空间变换矩阵
        /// </summary>
        public static readonly int VP_MATRIX_PROP_ID = Shader.PropertyToID("_VPMatrix");

        /// <summary>
        /// 当前颜色图
        /// </summary>
        public static readonly int CAMERA_COLOR_TEXTURE_PROP_ID = Shader.PropertyToID("_CameraColorTexture");

        /// <summary>
        /// 当前深度图
        /// </summary>
        public static readonly int CAMERA_DEPTH_TEXTURE_PROP_ID = Shader.PropertyToID("_CameraDepthTexture");

        /// <summary>
        /// 保存每个像素的反射点
        /// </summary>
        public static readonly int REFLECT_HASH_TEXTURE_PROP_ID = Shader.PropertyToID("_ReflectHashTexture");
        public static readonly int RW_REFLECT_HASH_TEXTURE_PROP_ID = Shader.PropertyToID("_RWReflectHashTexture");

        public static readonly int REFLECT_MAPPING_0_TEXTURE_PROP_ID = Shader.PropertyToID("_ReflectMapping0Texture");
        public static readonly int RW_REFLECT_MAPPING_0_TEXTURE_PROP_ID = Shader.PropertyToID("_RWReflectMapping0Texture");

        public static readonly int REFLECT_MAPPING_1_TEXTURE_PROP_ID = Shader.PropertyToID("_ReflectMapping1Texture");
        public static readonly int RW_REFLECT_MAPPING_1_TEXTURE_PROP_ID = Shader.PropertyToID("_RWReflectMapping1Texture");

        public static readonly int REFLECT_TEXTURE_PROP_ID = Shader.PropertyToID("_ReflectionTexture");
        public static readonly int RW_REFLECT_TEXTURE_PROP_ID = Shader.PropertyToID("_RWReflectionTexture");
    }

    private static readonly string PROFILE_TAG = "Screen Space Planar Reflection";

    /// <summary>
    /// 线程组数，必须与CS一致
    /// </summary>
    private static readonly int THREAD_GROUP_BITS = 3;

    private ComputeShader m_ReflectionShader;

    public bool isVaild { get { return m_ReflectionShader != null; } }

    private RenderTargetIdentifier m_ReflectHashTextureID = new RenderTargetIdentifier(ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID);
    private RenderTargetIdentifier m_ReflectMapping0TextureID = new RenderTargetIdentifier(ShaderConstants.REFLECT_MAPPING_0_TEXTURE_PROP_ID);
    private RenderTargetIdentifier m_ReflectMapping1TextureID = new RenderTargetIdentifier(ShaderConstants.REFLECT_MAPPING_1_TEXTURE_PROP_ID);
    private RenderTargetIdentifier m_TempReflectionTextureID = new RenderTargetIdentifier(ShaderConstants.REFLECT_TEXTURE_PROP_ID);
    private RenderTargetIdentifier m_ReflectionTextureID = new RenderTargetIdentifier(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);

    public ScreenSpacePlanarReflectionPass(ReflectionRendererFeature owner) : base(owner)
    {
#if UNITY_EDITOR
        m_ReflectionShader = Resources.Load<ComputeShader>("Shader/Reflection/ScreenSpacePlanarReflection");
#else
        m_ReflectionShader = AssetManager.instance.LoadAsset<ComputeShader>("Shader/Reflection/ScreenSpacePlanarReflection");
#endif
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        Vector2Int size = GetTextureSize(m_Onwer.quality, cameraTextureDescriptor.width, cameraTextureDescriptor.height);
        RenderTextureDescriptor desc = cameraTextureDescriptor;
        desc.colorFormat = RenderTextureFormat.ARGB32;
        desc.width = size.x;
        desc.height = size.y;
        desc.depthBufferBits = 0;
        desc.autoGenerateMips = false;
        desc.useMipMap = false;
        desc.enableRandomWrite = true;

        cmd.GetTemporaryRT(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID, desc, FilterMode.Bilinear);
        cmd.GetTemporaryRT(ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID, desc, FilterMode.Point);
        cmd.GetTemporaryRT(ShaderConstants.REFLECT_MAPPING_0_TEXTURE_PROP_ID, desc, FilterMode.Point);
        cmd.GetTemporaryRT(ShaderConstants.REFLECT_MAPPING_1_TEXTURE_PROP_ID, desc, FilterMode.Point);

        if (m_Onwer.SSPRSetting.fillHoles)
            cmd.GetTemporaryRT(ShaderConstants.REFLECT_TEXTURE_PROP_ID, desc, FilterMode.Point);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        {
            Camera camera = renderingData.cameraData.camera;

            cmd.Clear();
            cmd.BeginSample(PROFILE_TAG);
            context.ExecuteCommandBuffer(cmd);

            ReflectionPlane sharePlane = null;
            float planeDist = 0.0f;

            List<ReflectionPlane> planes = ReflectionManager.instance.planes;
            for (int i = 0; i < planes.Count; ++i)
            {
                var reflectionPlane = planes[i];
                if (reflectionPlane.isAlone)
                {
                    Vector3 planeNormal = reflectionPlane.transform.up, planePoint = reflectionPlane.transform.position;
                    Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector2.Dot(planeNormal, planePoint));
                    RenderReflectionTexture(context, ref renderingData, cmd, plane, ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);
                }
                else
                {
                    // 获取离相机最近的反射平面
                    if (sharePlane)
                    {
                        float d = Vector3.Distance(camera.transform.position, reflectionPlane.transform.position);
                        if (d < planeDist)
                        {
                            planeDist = d;
                            sharePlane = reflectionPlane;
                        }
                    }
                    else
                    {
                        sharePlane = reflectionPlane;
                    }
                }
            }

            if (sharePlane)
            {
                Vector3 planeNormal = sharePlane.transform.up, planePoint = sharePlane.transform.position;
                Vector4 plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, -Vector2.Dot(planeNormal, planePoint));
                RenderReflectionTexture(context, ref renderingData, cmd, plane, ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);
            }

            cmd.Clear();
            cmd.EndSample(PROFILE_TAG);
            context.ExecuteCommandBuffer(cmd);
        }
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.REFLECT_MAPPING_0_TEXTURE_PROP_ID);
        cmd.ReleaseTemporaryRT(ShaderConstants.REFLECT_MAPPING_1_TEXTURE_PROP_ID);
        cmd.ReleaseTemporaryRT(ReflectionRendererFeature.REFLECTION_TEX_PROP_ID);

        if (m_Onwer.SSPRSetting.fillHoles)
            cmd.ReleaseTemporaryRT(ShaderConstants.REFLECT_TEXTURE_PROP_ID);
    }

    private void RenderReflectionTexture(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer cmd, Vector4 plane, RenderTargetIdentifier texture)
    {
        ref CameraData cameraData = ref renderingData.cameraData;
        ref ScriptableRenderer renderer = ref cameraData.renderer;
#if UNITY_2022_1_OR_NEWER
        RTHandle colorTarget = renderer.cameraColorTargetHandle;
        RTHandle depthTarget = renderer.cameraDepthTargetHandle;
#else
        RenderTargetIdentifier colorTarget = renderer.cameraColorTarget;
        RenderTargetIdentifier depthTarget = renderer.cameraDepthTarget;
#endif

        var setting = m_Onwer.SSPRSetting;
        Camera camera = cameraData.camera;
        Vector2Int size = GetTextureSize(m_Onwer.quality, camera.pixelWidth, camera.pixelHeight);

        int dispatchThreadGroupX = size.x >> THREAD_GROUP_BITS;
        int dispatchThreadGroupY = size.y >> THREAD_GROUP_BITS;
        int dispatchThreadGroupZ = 1;

        cmd.Clear();

        cmd.SetComputeVectorParam(m_ReflectionShader, ShaderConstants.COLOR_TEXTURE_SIZE_PROP_ID, new Vector4(size.x, size.y, 1.0f / size.x, 1.0f / size.y));
        cmd.SetComputeVectorParam(m_ReflectionShader, ShaderConstants.REFLECTION_PLANE_PROP_ID, plane);
        cmd.SetComputeFloatParam(m_ReflectionShader, ShaderConstants.FADE_OUT_TO_EDGE_PROP_ID, setting.fadeOutToEdge);

        // 裁剪空间变换矩阵
        Matrix4x4 viewProjectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        cmd.SetComputeMatrixParam(m_ReflectionShader, ShaderConstants.VP_MATRIX_PROP_ID, viewProjectionMatrix);

        // 保存每个像素的反射点
        int kernel = m_ReflectionShader.FindKernel("RenderReflectHash");
        cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_HASH_TEXTURE_PROP_ID, m_ReflectHashTextureID);
        cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_MAPPING_0_TEXTURE_PROP_ID, m_ReflectMapping0TextureID);
        cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_MAPPING_1_TEXTURE_PROP_ID, m_ReflectMapping1TextureID);
        cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);

        if (setting.useDoubleMapping)
        {
            kernel = m_ReflectionShader.FindKernel("PreRenderReflectMapping");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID, m_ReflectHashTextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_MAPPING_0_TEXTURE_PROP_ID, m_ReflectMapping0TextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);

            kernel = m_ReflectionShader.FindKernel("RenderReflectMapping");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.CAMERA_DEPTH_TEXTURE_PROP_ID, depthTarget);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID, m_ReflectHashTextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_MAPPING_0_TEXTURE_PROP_ID, m_ReflectMapping0TextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_MAPPING_1_TEXTURE_PROP_ID, m_ReflectMapping1TextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);
        }
        else
        {
            kernel = m_ReflectionShader.FindKernel("PreRenderReflectMapping");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_HASH_TEXTURE_PROP_ID, m_ReflectHashTextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_MAPPING_0_TEXTURE_PROP_ID, m_ReflectMapping1TextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);
        }

        if (setting.fillHoles)
        {
            kernel = m_ReflectionShader.FindKernel("RenderReflectionTexture");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.CAMERA_COLOR_TEXTURE_PROP_ID, colorTarget);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_MAPPING_1_TEXTURE_PROP_ID, m_ReflectMapping1TextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_TEXTURE_PROP_ID, m_TempReflectionTextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);

            kernel = m_ReflectionShader.FindKernel("FillHoles");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_TEXTURE_PROP_ID, m_TempReflectionTextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_TEXTURE_PROP_ID, m_ReflectionTextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);
        }
        else
        {
            kernel = m_ReflectionShader.FindKernel("RenderReflectionTexture");
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.CAMERA_COLOR_TEXTURE_PROP_ID, colorTarget);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.REFLECT_MAPPING_1_TEXTURE_PROP_ID, m_ReflectMapping1TextureID);
            cmd.SetComputeTextureParam(m_ReflectionShader, kernel, ShaderConstants.RW_REFLECT_TEXTURE_PROP_ID, m_ReflectionTextureID);
            cmd.DispatchCompute(m_ReflectionShader, kernel, dispatchThreadGroupX, dispatchThreadGroupY, dispatchThreadGroupZ);
        }

        context.ExecuteCommandBuffer(cmd);
    }
}
