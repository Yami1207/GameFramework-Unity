#ifndef __LIGHTING_HLSL__
#define __LIGHTING_HLSL__

#include "BSDF.hlsl"
#include "IBLSpecular.hlsl"

inline half3 LightingLambert(half3 lightColor, BxDFContext bxdfContext)
{
    return lightColor * bxdfContext.NoL_sat;
}

inline half3 LightingHalfLambert(half3 lightColor, BxDFContext bxdfContext)
{
    return lightColor * bxdfContext.NoL_01;
}

inline half3 LightingSpecular(half3 lightColor, CustomSurfaceData surfaceData, BxDFContext bxdfContext)
{
    half smoothness = exp2(10 * surfaceData.smoothness + 1);
    half spec = pow(max(bxdfContext.NoH, 0.00009), smoothness);
    return lightColor * spec * surfaceData.specular;
}

//--------------------------------------
// 次表面散射
//inline half3 LightingSubsurfaceScattering(Light light, CustomInputData inputData, CustomSurfaceData surfaceData)
//{
//    // https://www.alanzucconi.com/2017/08/30/fast-subsurface-scattering-1/
//    half3 backLightDir = inputData.normalWS * surfaceData.subsurfaceRadius + light.direction;
//    half backSSS = saturate(dot(inputData.viewDirectionWS, -backLightDir));
//    backSSS = backSSS * backSSS * backSSS;
    
//    half attenuation = light.distanceAttenuation * light.shadowAttenuation;
//    half3 color = attenuation * backSSS * light.color * surfaceData.subsurfaceColor;
//    return color;
//}

//--------------------------------------
// Phong lighting
inline half3 CalculateBlinnPhong(Light light, CustomInputData inputData, CustomSurfaceData surfaceData)
{
    BxDFContext bxdfContext = GetBxDFContext(inputData, light.direction);
    half3 lightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;
    
    // 漫反射
#if USING_HALF_LAMBERT
    half3 diffuseColor = LightingHalfLambert(lightColor, bxdfContext);
#else
    half3 diffuseColor = LightingLambert(lightColor, bxdfContext);
#endif
    
    // 高光
    half3 specularColor = LightingSpecular(lightColor, surfaceData, bxdfContext);

    return diffuseColor * surfaceData.albedo + specularColor;
}

inline half3 LightingPhongLighting(CustomInputData inputData, CustomSurfaceData surfaceData)
{
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    
    // GI
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    half3 giColor = inputData.bakedGI * surfaceData.albedo;
    
    // Main Light
    half3 mainLightColor = CalculateBlinnPhong(mainLight, inputData, surfaceData);
        
    return giColor + mainLightColor;
}

//--------------------------------------
// PBR lighting
inline half3 LightingIndirect(CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext, half3 bakedGI, half occlusion, TEXTURECUBE_PARAM( tex, texSampler), half4 hdr)
{
    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GetGlossyEnvironmentReflection(bxdfContext.R, brdfData.perceptualRoughness, TEXTURECUBE_ARGS(tex, texSampler), hdr);
    half fresnelTerm = Pow4(1.0 - bxdfContext.NoV);

    //half3 brdf = EnvBRDFApprox(brdfData.specularColor, brdfData.perceptualRoughness, bxdfContext.NoV_01);
    //return (indirectDiffuse * brdfData.diffuseColor + indirectSpecular * brdf) * occlusion;
    half3 color = GetEnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
    return color * occlusion;
}

inline half3 LightingIndirect(CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext, half3 bakedGI, half occlusion)
{
    return LightingIndirect(surfaceData, brdfData, bxdfContext, bakedGI, occlusion, TEXTURECUBE_ARGS(unity_SpecCube0, samplerunity_SpecCube0), unity_SpecCube0_HDR);
}

inline half3 LightDirect(Light light, CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext)
{
    half attenuation = light.distanceAttenuation * light.shadowAttenuation;
    half3 radiance = light.color * (attenuation * bxdfContext.NoL_sat);
    half3 brdf = brdfData.diffuseColor;
    brdf += brdfData.specularColor * DirectBRDFSpecular(brdfData, bxdfContext);
    return brdf * radiance;
}

inline half3 LightingPhysicallyBased(CustomInputData inputData, CustomSurfaceData surfaceData)
{
    CustomBRDFData brdfData = GetBRDFData(surfaceData);
    
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    
    // GI
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    half3 giColor = inputData.bakedGI * surfaceData.albedo;

    // Main Light
    half3 mainLightColor = 0;
    {
        BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
        half3 indirectColor = LightingIndirect(surfaceData, brdfData, bxdfContext, inputData.bakedGI, surfaceData.occlusion);
        half3 directColor = LightDirect(mainLight, surfaceData, brdfData, bxdfContext);
        mainLightColor = indirectColor + directColor;
    }

    return giColor + mainLightColor;
}

//--------------------------------------
// 自发光
inline half3 MixEmission(half3 fragColor, CustomSurfaceData surfaceData)
{
    return fragColor + surfaceData.emission;
}

#endif
