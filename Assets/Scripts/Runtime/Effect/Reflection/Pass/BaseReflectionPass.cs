using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class BaseReflectionPass : ScriptableRenderPass
{
    protected readonly ReflectionRendererFeature m_Onwer;

    protected Color m_ClearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    public BaseReflectionPass(ReflectionRendererFeature onwer)
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        m_Onwer = onwer;
    }

    public static Vector2Int GetTextureSize(ReflectionQuality quality, int pixelWidth, int pixelHeight)
    {
        int[] sizeArray = { 128, 256, 512 };
        int size = sizeArray[(int)quality];

        int width = 0, height = 0;
        if (pixelWidth > pixelHeight)
        {
            width = size;
            height = Mathf.CeilToInt(0.125f * ((float)pixelHeight / pixelWidth) * size) * 8;
        }
        else
        {
            width = Mathf.CeilToInt(0.125f * ((float)pixelWidth / pixelHeight) * size) * 8;
            height = size;
        }
        return new Vector2Int(width, height);
    }
}
