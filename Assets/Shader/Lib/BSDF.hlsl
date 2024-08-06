#ifndef __BSDF_HLSL__
#define __BSDF_HLSL__

// 绝缘体反射率
#define DIELECTRIC_SPEC half4(0.04, 0.04, 0.04, 1.0 - 0.04)

struct CustomBRDFData
{
	// F0 
    half3 fresnel0;

    half3 diffuseColor;

    half3 specularColor;
    
    half reflectivity;

	// 感性粗糙度
    half perceptualRoughness;

	// perceptualRoughness^2
	// Burley 在"Physically Based Shading at Disney"提出的建议，把美术提供的roughness进行平方后使用在NDF
    half roughness;

	// perceptualRoughness^4
    half roughness2;

	// perceptualRoughness^4 - 1
    half roughness2MinusOne;

	// roughness * 4.0 + 2.0
    half normalizationTerm;
    
    half grazingTerm;
};

CustomBRDFData GetBRDFData(half3 albedo, half metallic, half smoothness)
{
    half roughness = 1 - smoothness;
    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);

    CustomBRDFData data = (CustomBRDFData) 0;
    data.fresnel0 = lerp(DIELECTRIC_SPEC.rgb, albedo, metallic);
    data.diffuseColor = albedo * lerp(0.96, 0.0, metallic);
    data.specularColor = data.fresnel0;
    
    data.reflectivity = 1 - oneMinusReflectivity;
    data.grazingTerm = saturate(smoothness + data.reflectivity);

	// 粗糙度
    data.perceptualRoughness = roughness;
    data.roughness = max(data.perceptualRoughness * data.perceptualRoughness, 0.002);
    data.roughness2 = data.roughness * data.roughness;
    data.roughness2MinusOne = data.roughness2 - 1;
    data.normalizationTerm = data.roughness * 4 + 2;

    return data;
}

CustomBRDFData GetBRDFData(CustomSurfaceData surfaceData)
{
    return GetBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.smoothness);
}

struct BxDFContext
{
    float NoV; // -1 to 1
    float NoV_01; // 0 to 1
    float NoV_sat; // clamp 0 to 1
    float NoV_abs; // abs 0 to 1

    float NoL; // -1 to 1
    float NoL_01; // 0 to 1
    float NoL_sat; // clamp 0 to 1
    float NoL_abs; // abs 0 to 1

    float3 R; // ReflectVector

    float3 H; // half dir
    float NoH;
    float LoH;
};

BxDFContext GetBxDFContext(float3 normalWS, float3 viewDirectionWS, float3 lightDir)
{
    BxDFContext context = (BxDFContext)0;

    context.NoV = dot(normalWS, viewDirectionWS);
    context.NoV_01 = context.NoV * 0.5 + 0.5;
    context.NoV_sat = saturate(context.NoV);
    context.NoV_abs = abs(context.NoV);

    context.NoL = dot(normalWS, lightDir);
    context.NoL_01 = context.NoL * 0.5 + 0.5;
    context.NoL_sat = saturate(context.NoL);
    context.NoL_abs = abs(context.NoL);

    context.R = reflect(-viewDirectionWS, normalWS);

    context.H = SafeNormalize(viewDirectionWS + lightDir);
    context.NoH = saturate(dot(normalWS, context.H));
    context.LoH = saturate(dot(lightDir, context.H));

    return context;
}

BxDFContext GetBxDFContext(CustomInputData inputData, float3 lightDir)
{
    return GetBxDFContext(inputData.normalWS, inputData.viewDirectionWS, lightDir);
}

// 来源: UE 4.25 => EnvBRDFApprox
// 根据UE代码，在计算时传NoV为Clamp01，但在计算ClearCoat时传入NoV_abs01
inline half3 EnvBRDFApprox(half3 specularColor, half roughness, half NoV)
{
	// [ Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II" ]
	// Adaptation to fit our G term.
    const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
    half4 r = roughness * c0 + c1;
    half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    half2 AB = half2(-1.04, 1.04) * a004 + r.zw;

	// Anything less than 2% is physically impossible and is instead considered to be shadowing
	// Note: this is needed for the 'specular' show flag to work, since it uses a specularColor of 0
    AB.y *= saturate(50.0 * specularColor.g);
    return specularColor * AB.x + AB.y;
}

// Computes the specular term for EnvironmentBRDF
inline half3 GetEnvironmentBRDFSpecular(CustomBRDFData brdfData, half fresnelTerm)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    return half3(surfaceReduction * lerp(brdfData.specularColor, brdfData.grazingTerm, fresnelTerm));
}

inline half3 GetEnvironmentBRDF(CustomBRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm)
{
    half3 color = indirectDiffuse * brdfData.diffuseColor;
    color += indirectSpecular * GetEnvironmentBRDFSpecular(brdfData, fresnelTerm);
    return color;
}

inline half DirectBRDFSpecular(CustomBRDFData brdfData, BxDFContext bxdfContext)
{
	// GGX
	// D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
    float d = bxdfContext.NoH * bxdfContext.NoH * brdfData.roughness2MinusOne + 1.0;
    float d2 = d * d, LoH2 = bxdfContext.LoH * bxdfContext.LoH;
    half specularTerm = brdfData.roughness2 / (d2 * max(0.1, LoH2) * brdfData.normalizationTerm);

#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
	specularTerm = specularTerm - HALF_MIN;
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    return specularTerm;
}

#endif
