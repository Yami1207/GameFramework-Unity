#ifndef __CHARACTER_FUNCTIONS_HLSL__
#define __CHARACTER_FUNCTIONS_HLSL__

#include "../../Lib/Core.hlsl"
#include "CharacterTypes.hlsl"

half3 SampleCoolRampColor(half2 uv);
half3 SampleWarmRampColor(half2 uv);

// 获取面部xyz方向
inline HeadDirections GetCharacterHeadDirections()
{
    HeadDirections dir;
    dir.forward = normalize(UNITY_MATRIX_M._m02_m12_m22);
    dir.right = normalize(UNITY_MATRIX_M._m00_m10_m20);
    dir.up = normalize(UNITY_MATRIX_M._m01_m11_m21);
    return dir;
}

// 漫反射
inline half3 CalculateRampDiffuse(CustomSurfaceData surfaceData, RampDiffuseData rampDiffuseData, BxDFContext bxdfContext, Light light)
{
    half3 shadowColor = lerp(_G_ShadowColor, 1, light.shadowAttenuation);
    
    float u = bxdfContext.NoL_01 * smoothstep(-0.1, 0.2, rampDiffuseData.ao);
    float noAOMask = floor(0.1 + rampDiffuseData.ao);
    u = lerp(u, 1.0, noAOMask);

    half2 rampUV = half2(u, rampDiffuseData.ramp);
    half3 coolColor = SampleCoolRampColor(rampUV);
    half3 warmColor = SampleWarmRampColor(rampUV);
    half3 rampColor = lerp(coolColor, warmColor, 1);
    return rampColor * surfaceData.albedo * light.color * light.distanceAttenuation * shadowColor;
}

// 高光
inline half3 CalculateSpecularColor(CustomSurfaceData surfaceData, BxDFContext bxdfContext, Light light)
{
    half attenuation = light.shadowAttenuation * light.distanceAttenuation;
    half roughness = 1 - surfaceData.smoothness;
    half threshold = 1.03 - surfaceData.specularThreshold;
    
    half specular = pow(max(0.001, bxdfContext.NoH), surfaceData.shininess) * attenuation;
    specular = smoothstep(threshold - roughness, threshold + roughness, specular) * surfaceData.specular;
    
    return surfaceData.albedo * light.color * specular * surfaceData.specularColor;
}

// 边缘光
inline half3 CalculateRimLightColor(CustomSurfaceData surfaceData, CustomInputData inputData, RimLightData rimLightData, BxDFContext bxdfContext)
{
    float4 scaledScreenParams = GetScaledScreenParams();
    float linearEyeDepth = GetLinearEyeDepth(inputData.positionCS);
    
    // 采样偏移后的深度
    half rimWidth = rimLightData.width;
    {
        // 处理不同分辨率下边缘光等宽
        rimWidth *= 0.0005 * scaledScreenParams.y;
        rimWidth *= unity_CameraProjection._m11 * lerp(0.41425, 0.56497, unity_OrthoParams.w);
    
        // 近大远小
        rimWidth *= 10 * rsqrt(linearEyeDepth);
    }
    float indexOffsetX = -sign(cross(inputData.viewDirectionWS, inputData.normalWS).y) * rimWidth;
    uint2 index = clamp(inputData.positionCS.xy - 0.5 + float2(indexOffsetX, 0), 0, scaledScreenParams.xy - 1); // 避免出界
    float linearEyeOffsetDepth = GetLinearEyeDepth(SampleSceneDepth(index));

    // 通过深度差计算边缘光区域
    float depthDiff = linearEyeOffsetDepth - linearEyeDepth;
    half rim = saturate(floor(1.0 + depthDiff - rimLightData.threshold));
    
    // 配合菲涅尔处理边缘光
    half fresnel = pow(max(0.0001, 1 - bxdfContext.NoV_sat), rimLightData.edgeSoftness);
    rim = lerp(0, rim, fresnel);

    return surfaceData.albedo * rimLightData.color * rim;
}

inline half3 CalculateRimShadowColor(RimShadowData rimShadowData, BxDFContext bxdfContext)
{
    half rimShadow = saturate((1 - bxdfContext.NoV_sat) * rimShadowData.width);
    rimShadow = smoothstep(rimShadowData.feather, 1, rimShadow) * rimShadowData.intensity * 0.25;
    return lerp(1, 2.0 * rimShadowData.color, max(rimShadow, 0));
}

// 自发光
inline half3 GetEmission(CustomSurfaceData surfaceData, half threshold)
{
    float emissionMask = floor(1 + surfaceData.alpha - threshold);
    return surfaceData.albedo * surfaceData.emissionColor * max(0, emissionMask * surfaceData.emission);
}

#endif
