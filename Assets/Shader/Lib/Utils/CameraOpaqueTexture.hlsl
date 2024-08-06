#ifndef __CAMERA_OPAQUE_TEXTURE_HLSL__
#define __CAMERA_OPAQUE_TEXTURE_HLSL__

TEXTURE2D(_G_ScreenOpaqueTexture);
SAMPLER(sampler_G_ScreenOpaqueTexture);

inline half3 SampleScreen(float4 positionSS)
{
    return SAMPLE_TEXTURE2D(_G_ScreenOpaqueTexture, sampler_G_ScreenOpaqueTexture, positionSS.xy / positionSS.w).rgb;
}

#endif
