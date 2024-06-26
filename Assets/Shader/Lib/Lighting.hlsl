#ifndef __LIGHTING_HLSL__
#define __LIGHTING_HLSL__

#include "BSDF.hlsl"
#include "IBLSpecular.hlsl"

inline LightingData CreateLightingData(CustomInputData inputData, CustomSurfaceData surfaceData)
{
    LightingData lightingData;
    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = surfaceData.emission;
    lightingData.vertexLightingColor = 0;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;
    return lightingData;
}

inline half3 LightingLambert(half3 lightColor, BxDFContext bxdfContext)
{
    return lightColor * bxdfContext.NoL_sat;
}

inline half3 LightingHalfLambert(half3 lightColor, BxDFContext bxdfContext)
{
    return lightColor * bxdfContext.NoV_01;
}

///////////////////////////////////////////////////////////////////////////////
//                                PBR                                        //
///////////////////////////////////////////////////////////////////////////////
half3 LightingIndirect(CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext, half3 bakedGI, half occlusion, TEXTURECUBE_PARAM(tex, texSampler), half4 hdr)
{
    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GetGlossyEnvironmentReflection(bxdfContext.R, brdfData.perceptualRoughness, TEXTURECUBE_ARGS(tex, texSampler), hdr);
    half fresnelTerm = Pow4(1.0 - bxdfContext.NoV);

    //half3 brdf = EnvBRDFApprox(brdfData.specularColor, brdfData.perceptualRoughness, bxdfContext.NoV_01);
    //return (indirectDiffuse * brdfData.diffuseColor + indirectSpecular * brdf) * occlusion;
    half3 color = GetEnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
    return color * occlusion;
}

half3 LightingIndirect(CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext, half3 bakedGI, half occlusion)
{
    return LightingIndirect(surfaceData, brdfData, bxdfContext, bakedGI, occlusion, TEXTURECUBE_ARGS(unity_SpecCube0, samplerunity_SpecCube0), unity_SpecCube0_HDR);
}

half3 LightDirect(Light light, CustomSurfaceData surfaceData, CustomBRDFData brdfData, BxDFContext bxdfContext)
{
    half attenuation = light.distanceAttenuation * light.shadowAttenuation;
    half3 radiance = light.color * (attenuation * bxdfContext.NoL_sat);
    half3 brdf = brdfData.diffuseColor;
    brdf += brdfData.specularColor * DirectBRDFSpecular(brdfData, bxdfContext);
    return brdf * radiance;
}

half3 LightingPhysicallyBased(CustomInputData inputData, CustomSurfaceData surfaceData, Light light)
{
    CustomBRDFData brdfData = GetBRDFData(surfaceData);
    BxDFContext bxdfContext = GetBxDFContext(inputData, light.direction);
    
    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(light, inputData.normalWS, inputData.bakedGI);
    
    // 间接光
    half3 indirectColor = LightingIndirect(surfaceData, brdfData, bxdfContext, inputData.bakedGI, surfaceData.occlusion);
    
    // 直接光
    half3 directColor = LightDirect(light, surfaceData, brdfData, bxdfContext);
    
    return directColor + indirectColor;
}

///////////////////////////////////////////////////////////////////////////////
//                              自发光                                       //
///////////////////////////////////////////////////////////////////////////////
half3 MixEmission(half3 fragColor, CustomSurfaceData surfaceData)
{
    return fragColor + surfaceData.emission;
}

#endif
