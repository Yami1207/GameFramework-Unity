#ifndef __TRANSFORMS_HLSL__
#define __TRANSFORMS_HLSL__

inline float SampleDepth(float2 uv, in Texture2D<float> depthTexture, in SamplerState depthSampler)
{
    float depth = depthTexture.SampleLevel(depthSampler, uv, 0);
#if !UNITY_REVERSED_Z
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
#endif
    return depth;
}

// 屏幕空间转换成世界空间
inline float3 PositionSSToPositionWS(float2 uv, in Texture2D<float> depthTexture, in SamplerState depthSampler)
{
    // 获取屏幕空间
    float depth = SampleDepth(uv, depthTexture, depthSampler);
    float4 positionCS = float4(2.0 * uv - 1.0, depth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
	positionCS.y = -positionCS.y;
#endif

	// 屏幕空间转换成世界空间
    float4 positionWS = mul(UNITY_MATRIX_I_VP, positionCS);
    return positionWS.xyz / positionWS.w;
}

// 裁剪空间转换成屏幕空间
inline float4 PositionSSToPositionCS(float2 positionSS, float depth)
{
    float4 positionCS = float4(2.0 * positionSS - 1.0, depth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
	positionCS.y = -positionCS.y;
#endif
    return positionCS;
}

// 裁剪空间转换成世界空间
inline float3 PositionSSToPositionWS(float2 positionSS, float depth)
{
    float4 positionCS = PositionSSToPositionCS(positionSS, depth);
    float4 positionWS = mul(UNITY_MATRIX_I_VP, positionCS);
    return positionWS.xyz / positionWS.w;
}

#endif
