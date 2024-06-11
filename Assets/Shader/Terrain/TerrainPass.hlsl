#ifndef __TERRAIN_HLSL__
#define __TERRAIN_HLSL__

#include "TerrainInput.hlsl"

inline void InitializeSurfaceData(Varyings input, float4 splatControl, out CustomSurfaceData surfaceData)
{
    // uv
    float2 uvSplat0 = TRANSFORM_TEX(input.texcoord, _Splat0);
    float2 uvSplat1 = TRANSFORM_TEX(input.texcoord, _Splat1);
    float2 uvSplat2 = TRANSFORM_TEX(input.texcoord, _Splat2);
    float2 uvSplat3 = TRANSFORM_TEX(input.texcoord, _Splat3);
    
    // 
    half3 albedo = splatControl.r * SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat0).rgb;
    albedo += splatControl.g * SAMPLE_TEXTURE2D(_Splat1, sampler_Splat1, uvSplat1).rgb;
    albedo += splatControl.b * SAMPLE_TEXTURE2D(_Splat2, sampler_Splat2, uvSplat2).rgb;
    albedo += splatControl.a * SAMPLE_TEXTURE2D(_Splat3, sampler_Splat3, uvSplat3).rgb;
    
    // 法线
    half3 normalTS = 0;
    normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat0), _NormalScale0) * splatControl.r;
    normalTS += UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal1, sampler_Normal1, uvSplat1), _NormalScale1) * splatControl.g;
    normalTS += UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal2, sampler_Normal2, uvSplat2), _NormalScale2) * splatControl.b;
    normalTS += UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal3, sampler_Normal3, uvSplat3), _NormalScale3) * splatControl.a;
#if HAS_HALF
    normalTS.z += half(0.01);
#else
    normalTS.z += 1e-5f;
#endif
    normalTS = normalize(normalTS);
    
    surfaceData = (CustomSurfaceData) 0;
    surfaceData.albedo = albedo;
    surfaceData.alpha = 1;
    surfaceData.normalTS = normalTS;
    
    surfaceData.metallic = dot(splatControl, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
    surfaceData.smoothness = dot(splatControl, half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3));
    
    surfaceData.occlusion = 1;

}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, out CustomInputData inputData)
{
    inputData = (CustomInputData)0;
    inputData.positionWS = input.positionWSAndFog.xyz;

    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
    inputData.normalWS = TransformTangentToWorld(surfaceData.normalTS, inputData.tangentToWorld);
    
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.viewDirectionWS = viewDirWS;

	// 阴影值
    inputData.shadowCoord = GetShadowCoordInFragment(inputData.positionWS, input.shadowCoord);
    
    // 雾
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFog.w);
    
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS.xyz);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

Varyings vert(Attributes input)
{
    Varyings output;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float2 texcoord = (positionWS.xz - _TerrianParam.zw) / _TerrianParam.xy;
    positionWS.y = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, texcoord, 0.0).r;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionWSAndFog = float4(positionWS, ComputeFogFactor(output.positionCS.z));
    output.texcoord = texcoord;

    // 法线
    float3 normalOS = SAMPLE_TEXTURE2D_LOD(_VertexNormalMap, sampler_VertexNormalMap, texcoord, 0.0).xyz;
    normalOS = 2.0 * normalOS - 1.0;
    VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS);
    //float3 bitangentWS = float3(0, 0, 1);
    //float3 tangentWS = cross(normalWS, bitangentWS);
    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
    
    OUTPUT_SH(output.normalWS, output.vertexSH);
    
    // 阴影
    output.shadowCoord = GetShadowCoord(positionWS, output.positionCS);
				
    return output;
}

FragData frag(Varyings input)
{
    float4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.texcoord);
    half weight = dot(splatControl, half4(1, 1, 1, 1));
#if defined(TERRAIN_ADD_PASS)
    clip(weight == 0.0 ? -1 : 1);
#endif
    
    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, splatControl, surfaceData);

    CustomInputData inputData;
    InitializeInputData(input, surfaceData, inputData);
    
    half3 color = LightingPhysicallyBased(inputData, surfaceData);
//    CustomBRDFData brdfData = GetBRDFData(surfaceData);

//    Light mainLight = GetMainLight();
//    BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
//    half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
//    half3 diffuseColor = LightingLambert(attenuatedLightColor, bxdfContext);
    
//#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
//    half3 lighting = diffuseColor * MainLightRealtimeShadow(inputData.shadowCoord);
//#else
//    half3 lighting = diffuseColor;
//#endif
    
//    // 间接光
//    half3 indirectColor = inputData.bakedGI * surfaceData.albedo; //LightingIndirect(surfaceData, brdfData, bxdfContext, inputData.bakedGI, surfaceData.occlusion);
    
//    half3 color = lighting * surfaceData.albedo + indirectColor;
    color = MixFog(color, inputData.fogCoord);

    FragData output = (FragData) 0;
    output.color = half4(color, surfaceData.alpha);
    output.normal = float4(inputData.normalWS * 0.5 + 0.5, 0.0);
    return output;
}

#if defined(SHADOW_CASTER_PASS)

float3 _LightDirection;
float3 _LightPosition;

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    float2 texcoord = (positionWS.xz - _TerrianParam.zw) / _TerrianParam.xy;
    positionWS.y = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, texcoord, 0.0).r;

    // 法线
    float3 normalWS = SAMPLE_TEXTURE2D_LOD(_VertexNormalMap, sampler_VertexNormalMap, texcoord, 0.0).xyz;
    normalWS = 2.0 * normalWS - 1.0;

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    output.positionCS = positionCS;
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_Target
{
    return 0;
}

#endif

#endif
