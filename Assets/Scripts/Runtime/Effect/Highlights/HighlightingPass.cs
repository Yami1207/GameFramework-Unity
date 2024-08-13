using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HighlightingPass : ScriptableRendererFeature
{
    public enum RenderEvent
    {
        BeforeRenderingTransparents,
        AfterRenderingTransparents
    }

    public enum TexQuality
    {
        High = 0,
        Middle,
        Low,
    }

    private class CustomRenderPass : ScriptableRenderPass
    {
        private const string PROFILE_TAG = "Highlighting Pass";

        private static readonly int HIGHLIGHTS_TEX_PROP_ID = Shader.PropertyToID("_HighlightsTex");
        private static readonly int BLUR_V_HIGHLIGHTS_TEX_PROP_ID = Shader.PropertyToID("_BlurVHighlightsTex");
        private static readonly int BLUR_HIGHLIGHTS_TEX_PROP_ID = Shader.PropertyToID("_BlurHighlightsTex");

        private static readonly int BLUR_ITERATIONS_PROP_ID = Shader.PropertyToID("_BlurIterations");
        private static readonly int BLUR_PIXEL_OFFSET_PROP_ID = Shader.PropertyToID("_BlurPixelOffset");
        private static readonly int BLUR_INTENSITY_PROP_ID = Shader.PropertyToID("_BlurIntensity");

        private static readonly int HIGHLIGHT_COLOR_PROP_ID = Shader.PropertyToID("_HighlightColor");

        private HighlightingPass m_Owner;

        private Material m_RendererMaterial;
        public Material rendererMaterial
        {
            get
            {
                if (m_RendererMaterial == null)
                    m_RendererMaterial = new Material(Shader.Find("Hidden/Highlighting/Renderer"));
                return m_RendererMaterial;
            }
        }

        private Material m_BlurMaterial;
        public Material blurMaterial
        {
            get
            {
                if (m_BlurMaterial == null)
                    m_BlurMaterial = new Material(Shader.Find("Hidden/Highlighting/Blur"));
                return m_BlurMaterial;
            }
        }

        public CustomRenderPass(HighlightingPass owner)
        {
            m_Owner = owner;
            renderPassEvent = owner.renderPassEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int bits = (int)m_Owner.m_TexQuality;
            int width = cameraTextureDescriptor.width >> bits;
            int height = cameraTextureDescriptor.height >> bits;

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.width = width;
            desc.height = height;
            desc.msaaSamples = 1;
            cmd.GetTemporaryRT(HIGHLIGHTS_TEX_PROP_ID, desc);

            RenderTextureDescriptor blurDesc = cameraTextureDescriptor;
            blurDesc.colorFormat = RenderTextureFormat.R8;
            blurDesc.width = width;
            blurDesc.height = height;
            blurDesc.useMipMap = false;
            blurDesc.depthBufferBits = 0;
            blurDesc.msaaSamples = 1;
            blurDesc.sRGB = false;
            cmd.GetTemporaryRT(BLUR_V_HIGHLIGHTS_TEX_PROP_ID, blurDesc);
            cmd.GetTemporaryRT(BLUR_HIGHLIGHTS_TEX_PROP_ID, blurDesc);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref cameraData.renderer;
            Camera camera = cameraData.camera;
#if UNITY_2022_1_OR_NEWER
            RTHandle colorTarget = renderer.cameraColorTargetHandle;
            RTHandle depthTarget = renderer.cameraDepthTargetHandle;
#else
            RenderTargetIdentifier colorTarget = renderer.cameraColorTarget;
            RenderTargetIdentifier depthTarget = renderer.cameraDepthTarget;
#endif
            var renderers = m_Owner.m_RenderersToDraw;

            SetupMaterial();

            CommandBuffer cmd = CommandBufferPool.Get();
            {
                cmd.Clear();
                cmd.BeginSample(PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);

                // 渲染外发光物体
                cmd.Clear();
                CoreUtils.SetRenderTarget(cmd, HIGHLIGHTS_TEX_PROP_ID, depthTarget);
                cmd.ClearRenderTarget(false, true, Color.black);
                for (int i = 0; i < renderers.Count; ++i)
                    cmd.DrawRenderer(renderers[i], rendererMaterial);
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                cmd.SetRenderTarget(colorTarget, depthTarget);
                cmd.Blit(HIGHLIGHTS_TEX_PROP_ID, BLUR_V_HIGHLIGHTS_TEX_PROP_ID, blurMaterial, 0);
                if (m_Owner.m_TexQuality != TexQuality.High)
                {
                    cmd.Blit(BLUR_V_HIGHLIGHTS_TEX_PROP_ID, BLUR_HIGHLIGHTS_TEX_PROP_ID, blurMaterial, 1);
                    cmd.Blit(Texture2D.blackTexture, colorTarget, blurMaterial, 2);
                }
                else
                {
                    cmd.Blit(Texture2D.blackTexture, colorTarget, blurMaterial, 3);
                }
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();
                cmd.EndSample(PROFILE_TAG);
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(HIGHLIGHTS_TEX_PROP_ID);
            cmd.ReleaseTemporaryRT(BLUR_V_HIGHLIGHTS_TEX_PROP_ID);
            cmd.ReleaseTemporaryRT(BLUR_HIGHLIGHTS_TEX_PROP_ID);
        }

        private void SetupMaterial()
        {
            blurMaterial.SetFloat(BLUR_ITERATIONS_PROP_ID, m_Owner.m_BlurIterations);
            blurMaterial.SetFloat(BLUR_PIXEL_OFFSET_PROP_ID, m_Owner.m_BlurPixelOffset);
            blurMaterial.SetFloat(BLUR_INTENSITY_PROP_ID, m_Owner.m_BlurIntensity);
            blurMaterial.SetColor(HIGHLIGHT_COLOR_PROP_ID, m_Owner.m_HighlightColor);
        }
    }

    private CustomRenderPass m_ScriptablePass;

    [SerializeField]
    private RenderEvent m_RenderingEvent = RenderEvent.BeforeRenderingTransparents;
    private RenderPassEvent renderPassEvent
    {
        get
        {
            if (m_RenderingEvent == RenderEvent.BeforeRenderingTransparents)
                return RenderPassEvent.BeforeRenderingTransparents;
            return RenderPassEvent.AfterRenderingTransparents;
        }
    }

    [SerializeField]
    private TexQuality m_TexQuality = TexQuality.High;

    [SerializeField]
    [Range(0.0f, 50.0f)]
    private float m_BlurIterations = 28.0f;

    [SerializeField]
    [Range(0.0f, 0.2f)]
    private float m_BlurPixelOffset = 0.03f;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float m_BlurIntensity = 2.0f;

    [SerializeField]
    private Color m_HighlightColor = Color.white;

    private List<Renderer> m_RenderersToDraw = new List<Renderer>(16);

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(this);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        List<Highlighter> highlighters = HighlighterManager.instance.highlighters;
        if (highlighters.Count == 0)
            return;

        m_RenderersToDraw.Clear();
        for (int i = 0; i < highlighters.Count; ++i)
        {
            Renderer[] renderers = highlighters[i].renderers;
            if (renderers == null || renderers.Length == 0)
                continue;

            for (int j = 0; j < renderers.Length; ++j)
            {
                Renderer r = renderers[j];
                if (r == null || r.enabled == false) 
                    continue;
                m_RenderersToDraw.Add(r);
            }
        }

        if (m_RenderersToDraw.Count > 0)
            renderer.EnqueuePass(m_ScriptablePass);
    }
}
