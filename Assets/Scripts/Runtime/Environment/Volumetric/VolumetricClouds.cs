using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumetricClouds : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private static class ShaderConstants
        {
            public static readonly int VOLUMETRIC_CLOUDS_VARIABLES_PROP_ID = Shader.PropertyToID("VolumetricCloudsVariables");

            ///// <summary>
            ///// 云层海拔高度
            ///// </summary>
            //public static readonly int LOWEST_CLOUD_ALTITUDE_PROP_ID = Shader.PropertyToID("_LowestCloudAltitude");

            ///// <summary>
            ///// 云层顶部海拔高度
            ///// </summary>
            //public static readonly int HIGHEST_CLOUD_ALTITUDE_PROP_ID = Shader.PropertyToID("_HighestCloudAltitude");

            //public static readonly int CLOUDS_TEXTURE_SIZE_PROP_ID = Shader.PropertyToID("_CloudsTextureSize");

            //public static readonly int DEPTH_TEXTURE_SIZE_PROP_ID = Shader.PropertyToID("_DepthTextureSize");

            //public static readonly int BASE_COLOR_PROP_ID = Shader.PropertyToID("_BaseColor");

            //public static readonly int CLOUD_HEIGHT_PROP_ID = Shader.PropertyToID("_CloudHeight");

            //public static readonly int CLOUD_THICKNESS_PROP_ID = Shader.PropertyToID("_CloudThickness");

            //public static readonly int DENSITY_TEXTURE_PROP_ID = Shader.PropertyToID("_DensityTexture");
            //public static readonly int DENSITY_TEXTURE_SCALE_PROP_ID = Shader.PropertyToID("_DensityTextureScale");
            //public static readonly int DENSITY_TEXTURE_OFFSET_PROP_ID = Shader.PropertyToID("_DensityTextureOffset");

            //public static readonly int kStepCountPropID = Shader.PropertyToID("_StepCount");

            public static readonly int HALF_DEPTH_TEXTURE_PROP_ID = Shader.PropertyToID("_HalfDepthTexture");
            public static readonly int RW_HALF_DEPTH_TEXTURE_PROP_ID = Shader.PropertyToID("_RWHalfDepthTexture");

            public static readonly int CLOUDS_TEXTURE_PROP_ID = Shader.PropertyToID("_CloudsTexture");
            public static readonly int RW_CLOUDS_TEXTURE_PROP_ID = Shader.PropertyToID("_RWCloudsTexture");

            /// <summary>
            /// 
            /// </summary>
            public static readonly int CLOUD_MASK_TEXTURE_PROP_ID = Shader.PropertyToID("_CloudMaskTexture");

            /// <summary>
            /// 云层分布图
            /// </summary>
            public static readonly int CLOUD_LUT_TEXTURE_PROP_ID = Shader.PropertyToID("_CloudLutTexture");

            /// <summary>
            /// 密度噪声图
            /// </summary>
            public static readonly int DENSITY_NOISE_TEXTURE_PROP_ID = Shader.PropertyToID("_DensityNoiseTexture");

            /// <summary>
            /// 侵蚀噪声图
            /// </summary>
            public static readonly int EROSION_NOISE_TEXTURE_PROP_ID = Shader.PropertyToID("_ErosionNoiseTexture");

            /// <summary>
            /// 相机ColorFrame
            /// </summary>
            public static readonly int CAMERA_COLOR_TEXTURE_PROP_ID = Shader.PropertyToID("_CameraColorTexture");

            public static readonly int COMBINE_COLOR_TEXTURE_PROP_ID = Shader.PropertyToID("_CombineColorTexture");
            public static readonly int RW_COMBINE_COLOR_TEXTURE_PROP_ID = Shader.PropertyToID("_RWCombineColorTexture");
        }

        #region Profile Tag

        private static readonly string VOLUMETRIC_CLOUDS_PROFILE_TAG = "Volumetric Clouds";

        private static readonly string DOWNSAMPLE_DEPTH_PROFILE_TAG = "Downsample Depth";

        private static readonly string RENDER_CLOUDS_PROFILE_TAG = "Render Clouds";

        private static readonly string COMBINE_COLOR_FRAME_PROFILE_TAG = "Combine Color Frame";

        #endregion

        public bool isVaild { get { return m_ComputeShader != null; } }

        private readonly VolumetricClouds m_Owner;

        private RenderTargetIdentifier m_HalfDepthTextureID = new RenderTargetIdentifier(ShaderConstants.HALF_DEPTH_TEXTURE_PROP_ID);

        private RenderTargetIdentifier m_CloudsTextureID = new RenderTargetIdentifier(ShaderConstants.CLOUDS_TEXTURE_PROP_ID);

        private RenderTargetIdentifier m_CombineColorTextureID = new RenderTargetIdentifier(ShaderConstants.COMBINE_COLOR_TEXTURE_PROP_ID);

        #region CS相关

        private ComputeShader m_ComputeShader = null;

        private int m_DownsampleDepthKernel = -1;
        private int m_RenderCloudsKernel = -1;
        private int m_CombineColorFrameKernel = -1;

        private VolumetricCloudsVariables m_CloudsCB;

        #endregion

        private Vector2Int m_CloudsTextureSize;

        public CustomRenderPass(VolumetricClouds owner)
        {
            m_Owner = owner;
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

#if UNITY_EDITOR
            m_ComputeShader = Resources.Load<ComputeShader>("Shader/Volumetric/VolumetricClouds");
#else
            m_ComputeShader = AssetManager.instance.LoadAsset<ComputeShader>("Shader/Volumetric/VolumetricClouds");
#endif
            if (m_ComputeShader != null)
            {
                m_DownsampleDepthKernel = m_ComputeShader.FindKernel("DownsampleDepth");
                m_RenderCloudsKernel = m_ComputeShader.FindKernel("RenderClouds");
                m_CombineColorFrameKernel = m_ComputeShader.FindKernel("CombineColorFrame");

                m_CloudsCB = new VolumetricCloudsVariables();
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_Owner.m_EnbaleDownsampleResolution)
            {
                m_CloudsTextureSize.x = Mathf.CeilToInt(0.5f * cameraTextureDescriptor.width);
                m_CloudsTextureSize.y = Mathf.CeilToInt(0.5f * cameraTextureDescriptor.height);
            }
            else
            {
                m_CloudsTextureSize.x = cameraTextureDescriptor.width;
                m_CloudsTextureSize.y = cameraTextureDescriptor.height;
            }

            RenderTextureDescriptor desc = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, cameraTextureDescriptor.colorFormat, 0);
            desc.useMipMap = false;
            desc.enableRandomWrite = true;
            cmd.GetTemporaryRT(ShaderConstants.COMBINE_COLOR_TEXTURE_PROP_ID, desc);

            // Clouds Texture
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            desc.width = m_CloudsTextureSize.x;
            desc.height = m_CloudsTextureSize.y;
            cmd.GetTemporaryRT(ShaderConstants.CLOUDS_TEXTURE_PROP_ID, desc);

            // Half Depth Texture
            if (m_Owner.m_EnbaleDownsampleResolution)
            {
                desc.colorFormat = RenderTextureFormat.RFloat;
                cmd.GetTemporaryRT(ShaderConstants.HALF_DEPTH_TEXTURE_PROP_ID, desc);
            }

            m_CloudsCB.cameraColorTextureSize = new Vector2Int(cameraTextureDescriptor.width, cameraTextureDescriptor.height);
            m_CloudsCB.halfDepthTextureSize = new Vector2Int(m_CloudsTextureSize.x, m_CloudsTextureSize.y);
            m_CloudsCB.cloudsTextureSize = new Vector4(m_CloudsTextureSize.x, m_CloudsTextureSize.y, 1.0f / m_CloudsTextureSize.x, 1.0f / m_CloudsTextureSize.y);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.BeginSample(VOLUMETRIC_CLOUDS_PROFILE_TAG);
                UpdateShaderVariableslClouds(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);

                if (m_Owner.m_EnbaleDownsampleResolution)
                {
                    cmd.Clear();
                    PerformDownsampleDepth(cmd, ref renderingData);
                    context.ExecuteCommandBuffer(cmd);
                }

                cmd.Clear();
                PerformRenderClouds(cmd);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                PerformCombineColorFrame(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                cmd.EndSample(VOLUMETRIC_CLOUDS_PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (m_Owner.m_EnbaleDownsampleResolution)
                cmd.ReleaseTemporaryRT(ShaderConstants.HALF_DEPTH_TEXTURE_PROP_ID);

            cmd.ReleaseTemporaryRT(ShaderConstants.CLOUDS_TEXTURE_PROP_ID);
            cmd.ReleaseTemporaryRT(ShaderConstants.COMBINE_COLOR_TEXTURE_PROP_ID);
        }

        private void UpdateShaderVariableslClouds(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_CloudsCB.cloudColor = m_Owner.m_CloudColor;
            m_CloudsCB.cloudMaskUVScale = m_Owner.m_CloudMaskUVScale;

            // 恒星半径
            float earthRadius = 1000.0f * m_Owner.m_EarthRadiusKm;
            m_CloudsCB.planetRadius = earthRadius * Mathf.Lerp(1.0f, 0.025f, m_Owner.m_EarthCurvature);

            // 云层海拔高度
            m_CloudsCB.cloudLayerAltitude = m_Owner.m_CloudLayerAltitude;
            m_CloudsCB.cloudLayerThickness = m_Owner.m_CloudLayerThickness;

            // 云层范围
            //float bottomRadius = m_CloudsCB.lowestCloudAltitude + m_CloudsCB.earthRadius;
            //float topRadius = m_CloudsCB.highestCloudAltitude + m_CloudsCB.earthRadius;
            //m_CloudsCB.cloudRangeSquared.Set(bottomRadius * bottomRadius, topRadius * topRadius);

            m_CloudsCB.shapeFactor = m_Owner.m_ShapeFactor;

            // 密度參數
            m_CloudsCB.densityNoiseScale = m_Owner.m_DensityNoiseScale;
            m_CloudsCB.densityMultiplier = 2.0f * m_Owner.m_DensityMultiplier * m_Owner.m_DensityMultiplier;

            // 侵蚀參數
            m_CloudsCB.erosionFactor = m_Owner.m_ErosionFactor;
            m_CloudsCB.erosionNoiseScale = m_Owner.m_ErosionNoiseScale;

            // Lighting
            m_CloudsCB.lightIntensity = m_Owner.m_LightIntensity;
            m_CloudsCB.multiScattering = 1.0f - m_Owner.m_MultiScattering * 0.95f;
            m_CloudsCB.powderEffectIntensity = m_Owner.m_PowderEffectIntensity;
            m_CloudsCB.extinction = Color.white - m_Owner.m_Extinction * m_Owner.m_ExtinctionScale;
            //m_CloudsCB.extinction = m_Owner.m_Extinction * m_Owner.m_ExtinctionScale;
            m_CloudsCB.erosionOcclusion = m_Owner.m_ErosionOcclusion;

            // HG参数
            m_CloudsCB.phaseG = m_Owner.m_PhaseG;
            m_CloudsCB.phaseG2 = m_Owner.m_PhaseG2;
            m_CloudsCB.phaseBlend = m_Owner.m_PhaseBlend;

            // ray步进数
            m_CloudsCB.numPrimarySteps = m_Owner.m_NumPrimarySteps;

            // 风向
            m_CloudsCB.windDirection = m_Owner.m_WindDirection;

            // 边缘淡出参数
            m_CloudsCB.fadeInStart = m_Owner.m_FadeInStart;
            m_CloudsCB.fadeInDistance = m_Owner.m_FadeInDistance;

            m_CloudsCB.useDownsampleResolution = m_Owner.m_EnbaleDownsampleResolution ? 1 : 0;

            ConstantBuffer.Push(cmd, m_CloudsCB, m_ComputeShader, ShaderConstants.VOLUMETRIC_CLOUDS_VARIABLES_PROP_ID);
        }

        private void PerformDownsampleDepth(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            Debug.Assert(m_DownsampleDepthKernel != -1);
#endif
            cmd.BeginSample(DOWNSAMPLE_DEPTH_PROFILE_TAG);
            {
                //Camera camera = renderingData.cameraData.camera;
                //cmd.SetComputeVectorParam(m_ComputeShader, ShaderConstants.DEPTH_TEXTURE_SIZE_PROP_ID, size);
                cmd.SetComputeTextureParam(m_ComputeShader, m_DownsampleDepthKernel, ShaderConstants.RW_HALF_DEPTH_TEXTURE_PROP_ID, m_HalfDepthTextureID);

                int x = Mathf.CeilToInt(0.125f * m_CloudsTextureSize.x), y = Mathf.CeilToInt(0.125f * m_CloudsTextureSize.y);
                cmd.DispatchCompute(m_ComputeShader, m_DownsampleDepthKernel, x, y, 1);
            }
            cmd.EndSample(DOWNSAMPLE_DEPTH_PROFILE_TAG);
        }

        private void PerformRenderClouds(CommandBuffer cmd)
        {
#if UNITY_EDITOR
            Debug.Assert(m_RenderCloudsKernel != -1);
#endif
            cmd.BeginSample(RENDER_CLOUDS_PROFILE_TAG);
            {
                CoreUtils.SetKeyword(cmd, "USE_DOWNSAMPLE_RESOLUTION", m_Owner.m_EnbaleDownsampleResolution);

                if (m_Owner.m_EnbaleDownsampleResolution)
                    cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.HALF_DEPTH_TEXTURE_PROP_ID, m_HalfDepthTextureID);
                cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.CLOUD_MASK_TEXTURE_PROP_ID, m_Owner.m_CloudMaskTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.CLOUD_LUT_TEXTURE_PROP_ID, m_Owner.m_CloudLutTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.DENSITY_NOISE_TEXTURE_PROP_ID, m_Owner.m_DensityNoiseTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.EROSION_NOISE_TEXTURE_PROP_ID, m_Owner.m_ErosionNoiseTexture);
                cmd.SetComputeTextureParam(m_ComputeShader, m_RenderCloudsKernel, ShaderConstants.RW_CLOUDS_TEXTURE_PROP_ID, m_CloudsTextureID);

                int x = Mathf.CeilToInt(0.125f * m_CloudsTextureSize.x), y = Mathf.CeilToInt(0.125f * m_CloudsTextureSize.y);
                cmd.DispatchCompute(m_ComputeShader, m_RenderCloudsKernel, x, y, 1);
            }
            cmd.EndSample(RENDER_CLOUDS_PROFILE_TAG);
        }

        private void PerformCombineColorFrame(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            Debug.Assert(m_CombineColorFrameKernel != -1);
#endif
            cmd.BeginSample(COMBINE_COLOR_FRAME_PROFILE_TAG);
            {
                ref CameraData cameraData = ref renderingData.cameraData;
                ref ScriptableRenderer renderer = ref cameraData.renderer;
#if UNITY_2022_1_OR_NEWER
                RTHandle colorTarget = renderer.cameraColorTargetHandle;
#else
                RenderTargetIdentifier colorTarget = renderer.cameraColorTarget;
#endif
                cmd.SetComputeTextureParam(m_ComputeShader, m_CombineColorFrameKernel, ShaderConstants.CLOUDS_TEXTURE_PROP_ID, m_CloudsTextureID);
                cmd.SetComputeTextureParam(m_ComputeShader, m_CombineColorFrameKernel, ShaderConstants.CAMERA_COLOR_TEXTURE_PROP_ID, colorTarget);
                cmd.SetComputeTextureParam(m_ComputeShader, m_CombineColorFrameKernel, ShaderConstants.RW_COMBINE_COLOR_TEXTURE_PROP_ID, m_CombineColorTextureID);

                int x = Mathf.CeilToInt(0.125f * m_CloudsCB.cameraColorTextureSize.x), y = Mathf.CeilToInt(0.125f * m_CloudsCB.cameraColorTextureSize.y);
                cmd.DispatchCompute(m_ComputeShader, m_CombineColorFrameKernel, x, y, 1);

                cmd.Blit(m_CombineColorTextureID, colorTarget);
            }
            cmd.EndSample(COMBINE_COLOR_FRAME_PROFILE_TAG);
        }

        private static float ComputeNormalizationFactor(float earthRadius, float lowerCloudRadius)
        {
            return Mathf.Sqrt((earthRadius + lowerCloudRadius) * (earthRadius + lowerCloudRadius) - earthRadius * earthRadius);
        }
    }

    private CustomRenderPass m_ScriptablePass;

    [SerializeField]
    private Color m_CloudColor = Color.white;

    [SerializeField]
    private Texture2D m_CloudMaskTexture;

    [SerializeField]
    private float m_CloudMaskUVScale = 1.0f;

    [SerializeField]
    private Texture2D m_CloudLutTexture;

    [SerializeField]
    private float m_CloudLayerAltitude = 1200.0f;

    [SerializeField]
    private float m_CloudLayerThickness = 2000.0f;

    [SerializeField]
    private float m_EarthRadiusKm = 6360.0f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_EarthCurvature = 0.0f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_ShapeFactor = 0.9f;

    // 密度參數
    [SerializeField]
    private Texture3D m_DensityNoiseTexture;

    [SerializeField]
    private float m_DensityNoiseScale = 0.05f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_DensityMultiplier = 1.0f;

    // 侵蚀參數
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_ErosionFactor = 0.8f;

    [SerializeField]
    private Texture3D m_ErosionNoiseTexture;

    [SerializeField]
    private float m_ErosionNoiseScale = 0.001f;

    // Lighting
    [SerializeField]
    private float m_LightIntensity = 0.7f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_MultiScattering = 0.5f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_PowderEffectIntensity = 0.7f;

    [SerializeField]
    private Color m_Extinction = new Color(0.71875f, 0.859375f, 1.0f, 0.0f);

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_ExtinctionScale = 0.05f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_ErosionOcclusion = 0.1f;

    // HG Phase Function
    [SerializeField]
    [Range(-1.0f, 1.0f)]
    private float m_PhaseG = 0.5f;

    [SerializeField]
    [Range(-1.0f, 1.0f)]
    private float m_PhaseG2 = -0.5f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_PhaseBlend = 0.5f;

    //Quality
    [SerializeField]
    [Range(2, 96)]
    private int m_NumPrimarySteps = 32;

    [SerializeField]
    private float m_FadeInStart = 1000.0f;

    [SerializeField]
    private float m_FadeInDistance = 20.0f;

    [SerializeField]
    private bool m_EnbaleDownsampleResolution = true;

    [SerializeField]
    private Vector2 m_WindDirection = new Vector2(1.0f, 1.0f);

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ScriptablePass.isVaild)
            renderer.EnqueuePass(m_ScriptablePass);
    }
}
