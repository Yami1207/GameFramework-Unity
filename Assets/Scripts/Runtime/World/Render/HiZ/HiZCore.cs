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
        public static readonly int prevMipmapPropID = Shader.PropertyToID("_PrevTexture");
        public static readonly int targetMipmapPropID = Shader.PropertyToID("_TargetTexture");

        public static readonly int depthTextureSizePropID = Shader.PropertyToID("_TextureSize");
    }

    private bool m_Enabled = true;
    public bool enabled { get { return m_Enabled; } }

    private ComputeShader m_GenerateMipmapShader;

    private int m_GenerateMipmapKernel = -1;

    /// <summary>
    /// 拷贝深度信息材质
    /// </summary>
    private Material m_CopyDepthMaterial;

    private DepthQuality m_DepthQuality = DepthQuality.High;

    private int m_DepthRTSize = 0;

    private int m_DepthRTMipCount = 0;

    /// <summary>
    /// 深度RT
    /// </summary>
    private RenderTexture m_DepthRT;

    public void Destroy()
    {
        m_Enabled = false;

        m_DepthRTSize = 0;
        m_DepthRTMipCount = 0;
    }

    public void ExecuteCopyDepth(CommandBuffer cmd, DepthQuality quality)
    {
        m_Enabled = true;
        m_DepthQuality = quality;

        // 检查资源
        CheckResourcesIfNeed();
        
        // 拷贝深度
        cmd.Blit(Texture2D.blackTexture, m_DepthRT, m_CopyDepthMaterial);

        // 生成mipmap数据
        int w = m_DepthRTSize, h = m_DepthRTSize >> 1;
        for (int i = 1; i < m_DepthRTMipCount; ++i)
        {
            w = Mathf.Max(1, w >> 1);
            h = Mathf.Max(1, h >> 1);

            cmd.SetComputeTextureParam(m_GenerateMipmapShader, m_GenerateMipmapKernel, ShaderConstants.prevMipmapPropID, m_DepthRT, i - 1);
            cmd.SetComputeTextureParam(m_GenerateMipmapShader, m_GenerateMipmapKernel, ShaderConstants.targetMipmapPropID, m_DepthRT, i);
            cmd.SetComputeVectorParam(m_GenerateMipmapShader, ShaderConstants.depthTextureSizePropID, new Vector4(w, h, 0f, 0f));

            int x, y;
            x = Mathf.CeilToInt(0.125f * w);
            y = Mathf.CeilToInt(0.125f * h);
            cmd.DispatchCompute(m_GenerateMipmapShader, m_GenerateMipmapKernel, x, y, 1);
        }
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
        if (width != m_DepthRTSize && m_DepthRT != null)
        {
            GameObject.Destroy(m_DepthRT);
            m_DepthRT = null;
        }

        if (m_DepthRT == null)
        {
            m_DepthRTSize = width;
            m_DepthRTMipCount = 8 - (int)(m_DepthQuality);

            m_DepthRT = new RenderTexture(m_DepthRTSize, m_DepthRTSize >> 1, 0, RenderTextureFormat.RHalf, m_DepthRTMipCount);
            m_DepthRT.name = "HiZDepthRT";
            m_DepthRT.useMipMap = true;
            m_DepthRT.autoGenerateMips = false;
            m_DepthRT.enableRandomWrite = true;
            m_DepthRT.wrapMode = TextureWrapMode.Clamp;
            m_DepthRT.filterMode = FilterMode.Point;
            m_DepthRT.Create();
        }
    }
}
