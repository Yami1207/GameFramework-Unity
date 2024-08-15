#ifndef __CAMERA_OPAQUE_TEXTURE_HLSL__
#define __CAMERA_OPAQUE_TEXTURE_HLSL__

TEXTURE2D(_G_ScreenOpaqueTexture);
SAMPLER(sampler_G_ScreenOpaqueTexture);
float4 _G_ScreenOpaqueTexture_TexelSize;

inline half3 SampleScreen(float2 positionSS)
{
    return SAMPLE_TEXTURE2D(_G_ScreenOpaqueTexture, sampler_G_ScreenOpaqueTexture, positionSS.xy).rgb;
}

inline half3 SampleScreen(float4 positionSS)
{
    return SampleScreen(positionSS.xy / positionSS.w);
}

#endif
