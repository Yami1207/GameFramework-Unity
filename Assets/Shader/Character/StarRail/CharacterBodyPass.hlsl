#ifndef __CHARACTER_BODY_PASS_HLSL__
#define __CHARACTER_BODY_PASS_HLSL__

#include "CharacterInput.hlsl"

inline void InitializeSurfaceData(Varyings input, half facing, uint rampLevel, out CustomSurfaceData surfaceData)
{
    half4 frontAlbedoAlpha = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.texcoord.xy) * _Color;
    half4 backAlbedoAlpha = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.texcoord.zw) * _BackColor;
    half4 albedoAlpha = lerp(backAlbedoAlpha, frontAlbedoAlpha, facing);

    surfaceData = (CustomSurfaceData) 0;
    surfaceData.albedo = albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
    
#if USING_MATERIAL_VALUES_PACK_LUT
    half4 specularColor = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 0));
    half4 specularValues = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 1));

    // 高光
    surfaceData.specularColor = specularColor.rgb;
    surfaceData.specular = specularValues.b;
    
    // 光泽度
    surfaceData.shininess = specularValues.r;;
    
    // 平滑度
    surfaceData.smoothness = 1 - specularValues.g;
#else
    // 高光
    surfaceData.specularColor = _SpecularColor;
    surfaceData.specular = _SpecularIntensity;
    
    // 光泽度
    surfaceData.shininess = _SpecularShininess;
    
    // 平滑度
    surfaceData.smoothness = 1 - _SpecularRoughness;
#endif
    
    // 自发光
    surfaceData.emission = surfaceData.alpha * _EmissionIntensity;
    surfaceData.emissionColor = _EmissionColor.rgb;
    
    // 顶点色
    surfaceData.vertexColor = input.color;
}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, inout CustomInputData inputData)
{
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = input.normalWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.viewDirectionWS = viewDirWS;

	// 阴影值
    inputData.shadowCoord = GetShadowCoordInFragment(inputData.positionWS, input.shadowCoord);
    
    // 雾
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndSH9.w);
    
    inputData.bakedGI = SampleSHPixel(input.fogFactorAndSH9.xyz, inputData.normalWS.xyz);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SampleShadowMask();
}

inline RampDiffuseData GetRampDiffuseData(CustomSurfaceData surfaceData, half ao, half rampLevel)
{
    RampDiffuseData rampDiffuseData;
    rampDiffuseData.ao = (ao + ao);// * surfaceData.vertexColor.r;
    rampDiffuseData.ramp = rampLevel;
    return rampDiffuseData;
}

inline RimLightData GetRimLightData(uint rampLevel)
{
    RimLightData rimLightData;
#if USING_MATERIAL_VALUES_PACK_LUT
    half4 rimLightColor = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 3));
    half4 rimLightValues = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 4));

    rimLightData.color = rimLightColor.rgb;
    rimLightData.width = rimLightValues.r;
    rimLightData.threshold = rimLightValues.g;
    rimLightData.edgeSoftness = rimLightValues.b;
#else
    rimLightData.color = _RimColor.rgb;
    rimLightData.width = _RimWidth;
    rimLightData.threshold = _RimLightThreshold;
    rimLightData.edgeSoftness = _RimLightEdgeSoftness;
#endif

    return rimLightData;
}

inline RimShadowData GetRimShadowData(uint rampLevel)
{
    RimShadowData rimShadowData;
#if USING_MATERIAL_VALUES_PACK_LUT
    half4 rimShadowColor = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 5));
    half4 rimShadowValues = LOAD_TEXTURE2D(_MaterialValuesPackLUT, uint2(rampLevel, 6));

    rimShadowData.color = rimShadowColor.rgb;
    rimShadowData.intensity = rimShadowValues.b;
    rimShadowData.width = rimShadowValues.r;
    rimShadowData.feather = rimShadowValues.g;
#else
    rimShadowData.color = _RimShadowColor.rgb;
    rimShadowData.intensity = _RimShadowIntensity;
    rimShadowData.width = _RimShadowWidth;
    rimShadowData.feather = _RimShadowFeather;
#endif

    return rimShadowData;
}

Varyings vert(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    Varyings output = (Varyings) 0;
    output.positionCS = vertexInput.positionCS;
    output.texcoord = half4(input.texcoord0, input.texcoord1);
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
    
    // sh与雾
    OUTPUT_SH(normalInput.normalWS, output.fogFactorAndSH9);
    output.fogFactorAndSH9.w = ComputeFogFactor(vertexInput.positionCS.z);
    
    // 阴影
    output.shadowCoord = GetShadowCoord(vertexInput.positionWS.xyz, output.positionCS);
    
    return output;
}

FragData frag(Varyings input, half facing : VFACE)
{
    half4 mask = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.texcoord.xy);
    half ramp = floor(mask.a * 8.0) * 0.125;
    uint ramplevel = 8 * frac(ramp);
    
    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, facing, ramplevel, surfaceData);
    
    CustomInputData inputData = GetDefaultInputData();
    InitializeInputData(input, surfaceData, inputData);
    
    RampDiffuseData rampDiffuseData = GetRampDiffuseData(surfaceData, mask.g, ramp);
    RimLightData rimLightData = GetRimLightData(ramplevel);
    RimShadowData rimShadowData = GetRimShadowData(ramplevel);
    
    half4 shadowMask = CalculateShadowMask(inputData);
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    
    // 主光
    half3 mainLightColor = 0;
    {
        BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
    
        // 漫反射
        mainLightColor = CalculateRampDiffuse(surfaceData, rampDiffuseData, bxdfContext, mainLight);
        
        // 高光
#if USING_SPECULAR
        mainLightColor += CalculateSpecularColor(surfaceData, bxdfContext, mainLight);
#endif
        
        // 边缘光
#if USING_RIM_LIGHT
        mainLightColor += CalculateRimLightColor(surfaceData, inputData, rimLightData, bxdfContext);
#endif
        
        // Rim Shadow Color
#if USING_RIM_SHADOW
        mainLightColor *= CalculateRimShadowColor(rimShadowData, bxdfContext);
#endif
    }
    
    // 自发光
    half3 emissionColor = 0;
#if USING_EMISSION
    emissionColor = GetEmission(surfaceData, _EmissionThreshold);
#endif
    
    half3 finalColor = mainLightColor + emissionColor;
    
    // 与雾混合
    finalColor = MixFog(finalColor, inputData, surfaceData);
    
    FragData output = (FragData) 0;
    output.color = half4(finalColor, surfaceData.alpha);
    output.normal = float4(input.normalWS.xyz * 0.5 + 0.5, 0.0);
    return output;
}

#endif
