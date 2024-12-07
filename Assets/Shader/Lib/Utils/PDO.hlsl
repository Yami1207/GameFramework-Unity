#ifndef __PIXEL_DEPTH_OFFSET_HLSL__
#define __PIXEL_DEPTH_OFFSET_HLSL__

TEXTURE2D(_G_PDO_AlbedoTex);
SAMPLER(sampler_G_PDO_AlbedoTex);

TEXTURE2D(_G_PDO_NormalTex);
SAMPLER(sampler_G_PDO_NormalTex);

inline void MixPixelDepthOffset(float3 positionWS, half differ, inout CustomSurfaceData surfaceData, inout CustomInputData inputData)
{   
    // 由于使用同一个相机,可用裁剪空间坐标做为UV
    float4 positionCS = TransformWorldToHClip(positionWS);
    half2 uv = 0.5 * (positionCS.xy / positionCS.w) + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1 - uv.y;
#endif
    
    // albedo
    float4 albedoColor = SAMPLE_TEXTURE2D(_G_PDO_AlbedoTex, sampler_G_PDO_AlbedoTex, uv);    
    float depth = LinearEyeDepth(positionWS, GetWorldToViewMatrix());
    half t = smoothstep(0, differ, albedoColor.a - depth); //saturate((albedoColor.a - depth) / differ);
    surfaceData.albedo = lerp(albedoColor.rgb, surfaceData.albedo, t);
    
    surfaceData.metallic = lerp(0, surfaceData.metallic, t);
    surfaceData.smoothness = lerp(0, surfaceData.smoothness, t);
    surfaceData.emission = lerp(0, surfaceData.emission, t);
    surfaceData.emissionColor = lerp(0, surfaceData.emissionColor, t);

    // nromal
    float3 normalWS = SAMPLE_TEXTURE2D(_G_PDO_NormalTex, sampler_G_PDO_NormalTex, uv).rgb;
    normalWS = 2.0 * normalWS - 1.0;
    inputData.normalWS = lerp(normalWS, inputData.normalWS, t);
    
    // 处理SH
    inputData.bakedGI = SampleSH(inputData.normalWS);
}

#endif
