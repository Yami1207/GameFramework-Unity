using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static HiZCore;
using static Unity.Burst.Intrinsics.X86.Avx;

public class HiZCore : Singleton<HiZCore>
{
    public enum DepthQuality
    {
        High = 0,
        Middle,
        Low,
    }

    private static class ShaderConstants
    {
        public static readonly int PREV_MIPMAP_PROP_ID = Shader.PropertyToID("_PrevTexture");

        public static readonly int TARGET_MIPMAP_PROP_ID = Shader.PropertyToID("_TargetTexture");

        public static readonly int DEPTH_TEXTURE_SIZE_PROP_ID = Shader.PropertyToID("_TextureSize");

        public static readonly int HIZ_VAILD_PROP_ID = Shader.PropertyToID("_HiZVaild");

        public static readonly int DEPTH_VIEW_PROJECTION_PROP_ID = Shader.PropertyToID("_HiZViewProjection");

        public static readonly int DEPTH_TEXTURE_PROP_ID = Shader.PropertyToID("_HiZDepthTexture");

        public static readonly int DEPTH_TEXTURE_PARAMS_PROP_ID = Shader.PropertyToID("_HiZDepthTextureParams");
    }

    private int m_RenderFrame = 0;

    private ComputeShader m_GenerateMipmapShader;

    private int m_GenerateMipmapKernel = -1;

    /// <summary>
    /// 拷贝深度信息材质
    /// </summary>
    private Material m_CopyDepthMaterial;

    private DepthQuality m_DepthQuality = DepthQuality.High;

    /// <summary>
    /// 深度图
    /// </summary>
    private RenderTexture m_DepthTexture;

    /// <summary>
    /// 深度图大小
    /// </summary>
    private int m_DepthTextureSize = 0;

    /// <summary>
    /// 深度图mipmap数
    /// </summary>
    private int m_DepthTextureMipLevel = 0;

    /// <summary>
    /// 深度图的视角投影矩阵
    /// </summary>
    private Matrix4x4 m_ViewProjectionMatrix;

    private Vector4 m_DepthTextureParams = Vector4.zero;

    public bool isVaild { get { return m_DepthTexture != null && Time.renderedFrameCount - m_RenderFrame < 3;  } }

    public void Destroy()
    {
        m_DepthTextureSize = 0;
        m_DepthTextureMipLevel = 0;

        if (m_DepthTexture != null)
        {
            GameObject.Destroy(m_DepthTexture);
            m_DepthTexture = null;
        }
    }

    public void SetupShaderParams(Camera camera, ComputeShader shader, int[] kernels)
    {
        shader.SetInt(ShaderConstants.HIZ_VAILD_PROP_ID, isVaild ? 1 : 0);
        shader.SetMatrix(ShaderConstants.DEPTH_VIEW_PROJECTION_PROP_ID, m_ViewProjectionMatrix);
        shader.SetVector(ShaderConstants.DEPTH_TEXTURE_PARAMS_PROP_ID, m_DepthTextureParams);

        if (isVaild)
        {
            for (int i = 0; i < kernels.Length; ++i)
                shader.SetTexture(kernels[i], ShaderConstants.DEPTH_TEXTURE_PROP_ID, m_DepthTexture);
        }
        else
        {
            for (int i = 0; i < kernels.Length; ++i)
                shader.SetTexture(kernels[i], ShaderConstants.DEPTH_TEXTURE_PROP_ID, Texture2D.blackTexture);
        }
    }

    public void ExecuteCopyDepth(ref Camera camera, ref CommandBuffer cmd, DepthQuality quality)
    {
        if (m_RenderFrame == Time.renderedFrameCount)
        {
            Debug.LogError("同一帧不能多次调用，否则影响性能");
            return;
        }

        m_RenderFrame = Time.renderedFrameCount;
        m_DepthQuality = quality;

        // 检查资源
        CheckResourcesIfNeed();
        
        // 拷贝深度
        cmd.Blit(null, m_DepthTexture, m_CopyDepthMaterial);

        // 生成mipmap数据
        int w = m_DepthTextureSize, h = m_DepthTextureSize >> 1;
        for (int i = 1; i < m_DepthTextureMipLevel; ++i)
        {
            w = Mathf.Max(1, w >> 1);
            h = Mathf.Max(1, h >> 1);

            cmd.SetComputeTextureParam(m_GenerateMipmapShader, m_GenerateMipmapKernel, ShaderConstants.PREV_MIPMAP_PROP_ID, m_DepthTexture, i - 1);
            cmd.SetComputeTextureParam(m_GenerateMipmapShader, m_GenerateMipmapKernel, ShaderConstants.TARGET_MIPMAP_PROP_ID, m_DepthTexture, i);
            cmd.SetComputeVectorParam(m_GenerateMipmapShader, ShaderConstants.DEPTH_TEXTURE_SIZE_PROP_ID, new Vector4(w, h, 0f, 0f));

            int x, y;
            x = Mathf.CeilToInt(0.125f * w);
            y = Mathf.CeilToInt(0.125f * h);
            cmd.DispatchCompute(m_GenerateMipmapShader, m_GenerateMipmapKernel, x, y, 1);
        }

        // 记录当前视角投影矩阵
        var proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        m_ViewProjectionMatrix = proj * camera.worldToCameraMatrix;
    }

    private void CheckResourcesIfNeed()
    {
        if (m_GenerateMipmapShader == null)
        {
            m_GenerateMipmapShader = AssetManager.instance.LoadAsset<ComputeShader>("Shader/HiZ/GenerateMipmap");
            m_GenerateMipmapKernel = m_GenerateMipmapShader.FindKernel("CSMain");
        }
        Debug.Assert(m_GenerateMipmapShader != null);

        if (m_CopyDepthMaterial == null)
            m_CopyDepthMaterial = new Material(Shader.Find("Hidden/HiZ/CopyDepth"));

        CreateDepthRenderTexture();
    }

    private void CreateDepthRenderTexture()
    {
        int width = 1024 >> (int)(m_DepthQuality);
        if (width != m_DepthTextureSize && m_DepthTexture != null)
        {
            GameObject.Destroy(m_DepthTexture);
            m_DepthTexture = null;
        }

        if (m_DepthTexture == null)
        {
            m_DepthTextureSize = width;

            // 最小mipmap分辨率为2 * 1
            m_DepthTextureMipLevel = 10 - (int)(m_DepthQuality);

#if UNITY_ANDROID
            RenderTextureFormat depthTextureFormat = RenderTextureFormat.RFloat;
#else
            RenderTextureFormat depthTextureFormat = RenderTextureFormat.RHalf;
#endif
            m_DepthTexture = new RenderTexture(m_DepthTextureSize, m_DepthTextureSize >> 1, 0, depthTextureFormat, m_DepthTextureMipLevel);
            m_DepthTexture.name = "HiZDepthRT";
            m_DepthTexture.useMipMap = true;
            m_DepthTexture.autoGenerateMips = false;
            m_DepthTexture.enableRandomWrite = true;
            m_DepthTexture.wrapMode = TextureWrapMode.Clamp;
            m_DepthTexture.filterMode = FilterMode.Point;
            m_DepthTexture.Create();

            m_DepthTextureParams.Set(m_DepthTexture.width, m_DepthTexture.height, m_DepthTextureMipLevel, 0);
        }
    }
}
