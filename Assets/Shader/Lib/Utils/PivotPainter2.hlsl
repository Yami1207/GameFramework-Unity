#ifndef __PIVOT_PAINTER_2_HLSL__
#define __PIVOT_PAINTER_2_HLSL__

inline float4 PivotPainter2_SamplePivotAndIndex(float2 uv, TEXTURE2D_PARAM(textureName, samplerName))
{
    uv.y = 1 - uv.y;
    return G2L(SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uv, 0.0));
}

inline float3 PivotPainter2_ConvertCoord(float3 position, float unitScale)
{
    return position * float3(-1, 1, 1) * unitScale;
}

#endif
