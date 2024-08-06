using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceReflection : ScriptableRendererFeature
{
    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly string s_ProfileTag = "Screen Specular Reflection";

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }
    }

    private CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
    }

    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
