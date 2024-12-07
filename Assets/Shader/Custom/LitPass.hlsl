#ifndef __LIT_PASS_HLSL__
#define __LIT_PASS_HLSL__

#include "LitInput.hlsl"

inline void InitializeSurfaceData(Varyings input, out CustomSurfaceData surfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord);
    
    surfaceData = (CustomSurfaceData) 0;
    surfaceData.albedo = _BaseColor * albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
        
#if USING_BUMP_MAP
    surfaceData.normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.texcoord), _BumpScale);
#endif
    
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Smoothness;
    
    surfaceData.emission = _EmissionIntensity;
    surfaceData.emissionColor = _EmissionColor.rgb;
    surfaceData.occlusion = 1;
}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, out CustomInputData inputData)
{
    inputData = (CustomInputData) 0;
    
#if USING_BUMP_MAP
    // 世界坐标
    inputData.positionWS = float3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    
    // 法线
    float3 normalWS = mul(surfaceData.normalTS, float3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
    inputData.normalWS = SafeNormalize(normalWS);
#else
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
#endif

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

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
#if USING_BUMP_MAP
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
#else
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
#endif

    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = vertexInput.positionCS;
    output.texcoord = input.texcoord;
    
#if USING_BUMP_MAP
	output.normalWS = float4(normalInput.normalWS, vertexInput.positionWS.x);
	output.tangentWS = float4(normalInput.tangentWS, vertexInput.positionWS.y);
	output.bitangentWS = float4(normalInput.bitangentWS, vertexInput.positionWS.z);
#else
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
#endif

    // 阴影
    output.shadowCoord = GetShadowCoord(vertexInput.positionWS, output.positionCS);

    // sh and fog
    half fog = ComputeFogFactor(output.positionCS.z);
    half3 vertexSH = SampleSHVertex(output.normalWS.xyz);
    output.vertexSHAndFog = half4(vertexSH, fog);
        
    return output;
}

FragData frag(Varyings input)
{
    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, surfaceData);

    CustomInputData inputData;
    InitializeInputData(input, surfaceData, inputData);
    
#if defined(_ENABLE_MIX_TERRAIN) && defined(_PIXEL_DEPTH_OFFSET_ON)
    MixPixelDepthOffset(inputData.positionWS, _MixDepthDiffer, surfaceData, inputData);
#endif
    
    half3 color = LightingPhysicallyBased(inputData, surfaceData);
    
    // 自发光
    color = MixEmission(color, surfaceData);
    
    // 与雾混合
    color = MixFog(color, inputData, surfaceData);

    FragData output = (FragData) 0;
    output.color = half4(color, 1);
    output.normal = float4(inputData.normalWS * 0.5 + 0.5, 0.0);
    return output;
}

#endif
