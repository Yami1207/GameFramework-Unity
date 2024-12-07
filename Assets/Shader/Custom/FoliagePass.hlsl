#ifndef __FOLIAGE_PASS_HLSL__
#define __FOLIAGE_PASS_HLSL__

#include "FoliageInput.hlsl"

inline float3 GetVertexPosition(in float3 positionOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
#if USING_WIND
    positionWS += SimpleGrassWind(positionWS, 1);
#elif USING_WIND_WAVE
    // 风浪效果
    float4 windWave = SimpleWindWave(positionWS, 1);
    positionWS += windWave.xyz;
#endif

    return positionWS;
}

///////////////////////////////////////////////////////////////////////////////
//                               Forward                                      /
///////////////////////////////////////////////////////////////////////////////
inline void InitializeSurfaceData(Varyings input, inout CustomSurfaceData surfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord);
#if USING_ALPHA_CUTOFF
    clip(albedoAlpha.a - _AlphaCutoff);
#endif
    
#if USING_GRADIENT_COLOR
    float3 albedoMask = input.normalWS.y;
    albedoMask = albedoMask * 0.5 + 0.5;
    albedoMask *= _ColorMaskHeight;
    albedoMask = smoothstep(0, 1, albedoMask);
    half3 albedo = lerp(_BaseBottomColor, _BaseColor, albedoMask);
#else
    half3 albedo = _BaseColor;
#endif

    surfaceData.albedo = albedo * albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
    
    surfaceData.metallic = 0;
    surfaceData.smoothness = 0;
    
    surfaceData.emission = 0;
    surfaceData.emissionColor = half3(0, 0, 0);
    surfaceData.occlusion = input.color.r;
}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, inout CustomInputData inputData)
{
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.viewDirectionWS = viewDirWS;

	// 阴影值
    inputData.shadowCoord = GetShadowCoordInFragment(inputData.positionWS, input.shadowCoord);
    
    // 雾
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.vertexSHAndFog.w);
    
    inputData.bakedGI = SampleSHPixel(input.vertexSHAndFog.xyz, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    
    inputData.shadowMask = SampleShadowMask();
}

Varyings vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionWS = GetVertexPosition(input.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.texcoord = input.texcoord;
    output.color = input.color;
    
    output.positionWS = positionWS;
    output.normalWS = normalInput.normalWS;

    // 阴影
    output.shadowCoord = GetShadowCoord(positionWS, output.positionCS);

    // sh and fog
    half fog = ComputeFogFactor(output.positionCS.z);
    half3 vertexSH = SampleSHVertex(output.normalWS.xyz);
    output.vertexSHAndFog = half4(vertexSH, fog);
        
    return output;
}

FragData frag(Varyings input)
{
    CustomSurfaceData surfaceData = GetDefaultSurfaceData();
    InitializeSurfaceData(input, surfaceData);

    CustomInputData inputData = GetDefaultInputData();
    InitializeInputData(input, surfaceData, inputData);

    half3 color = LightingPhongLighting(inputData, surfaceData);
    
    // 次表面散射
#if _ENABLE_SSS_ON
    {
        Light mainLight = GetMainLight(inputData.shadowCoord);
        BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
        half rim = 1.0 - bxdfContext.NoV_abs;

        half3 backLightDir = inputData.normalWS * max(1 - _SubsurfaceRadius, 0.001) + mainLight.direction;
        half backSSS = saturate(dot(inputData.viewDirectionWS, -backLightDir));
        backSSS = backSSS * backSSS * backSSS;
    
        half3 subsurfaceColor = saturate(surfaceData.albedo - max(Max3(surfaceData.albedo) - 0.2, 0.1)) * _SubsurfaceColor * _SubsurfaceColorIntensity;
        color += rim * backSSS * mainLight.color * subsurfaceColor;
    }
#endif
    
    // 与雾混合
    color = MixFog(color, inputData, surfaceData);

    FragData output = (FragData) 0;
    output.color = half4(color, 1);
    output.normal = float4(inputData.normalWS * 0.5 + 0.5, 0.0);
    return output;
}

///////////////////////////////////////////////////////////////////////////////
//                             ShadowCaster                                   /
///////////////////////////////////////////////////////////////////////////////
float3 _LightDirection;
float3 _LightPosition;

Varyings ShadowPassVertex(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float3 positionWS = GetVertexPosition(input.positionOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
#if USING_ALPHA_CUTOFF
    output.texcoord = input.texcoord;
#endif

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = SafeNormalize(_LightPosition - vertexInput.positionWS);
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
#if USING_ALPHA_CUTOFF
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord.xy);
    clip(albedoAlpha.a - _AlphaCutoff);
#endif
    return 0;
}

///////////////////////////////////////////////////////////////////////////////
//                              DepthOnly                                     /
///////////////////////////////////////////////////////////////////////////////
Varyings DepthOnlyVertex(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
        
    float3 positionWS = GetVertexPosition(input.positionOS);
    
    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformWorldToHClip(positionWS);
#if USING_ALPHA_CUTOFF
    output.texcoord = input.texcoord;
#endif
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if USING_ALPHA_CUTOFF
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord.xy);
	clip(albedoAlpha.a - _AlphaCutoff);
#endif
    return 0;
}

#endif