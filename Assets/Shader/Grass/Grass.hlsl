#ifndef __GRASS_HLSL__
#define __GRASS_HLSL__

// ========================= 开关定义 =========================
#define USING_SWING (_USE_SWING)

#define USING_INTERACTIVE  (_USE_INTERACTIVE)

#define USING_SOLID_COLOR (USE_SOLID_COLOR)

#define USING_ALPHA_CUTOFF (USE_ALPHA_CUTOFF)

#include "../Lib/Core.hlsl"
#include "../Lib/Wind.hlsl"

//--------------------------------------
// 材质属性
uniform half3 _BaseColor;
uniform half3 _GrassTipColor;
uniform half3 _GrassShadowColor;
uniform half _Cutoff;

uniform half _Roughness;
uniform half _ReflectionIntensity;

uniform half _SwingFeq;
uniform half _SwingFeqMax;
uniform half _SwingScale;
uniform half _SwingAmp;

uniform half _GrassPivotPointTexUnit;
uniform half _GrassPushStrength;

//--------------------------------------
// 贴图
TEXTURE2D(_GrassPivotPointTex);
SAMPLER(sampler_GrassPivotPointTex);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    half2 texcoord      : TEXCOORD0;
    half2 texcoord2     : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half2 texcoord          : TEXCOORD0;
    float4 positionWSAndFog : TEXCOORD1;
    float3 normalWS         : TEXCOORD2;
    float4 shadowCoord      : TEXCOORD3;
    half3 vertexSH          : TEXCOORD4;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
    half4 color : SV_Target0;
    float4 normal : SV_Target1;
};

#include "../Lib/Instancing.hlsl"
#include "../Lib/Utils/ObjectTrails.hlsl"

inline float3 GetGrassPosition(Attributes input)
{
    float3 positionOS = input.positionOS;
    float lerpY = input.texcoord.y * input.texcoord.y;
    
#if USING_SWING
    float phase = UNITY_MATRIX_M._m10 - input.texcoord.x;
    float feq = lerp(_SwingFeq, _SwingFeqMax, GetWindIntensity() * _SwingScale);
    positionOS = SimpleSwingPositionOS(positionOS, feq, _SwingAmp, lerpY, phase);
#endif

    float3 positionWS = TransformObjectToWorld(positionOS);
#if USING_INTERACTIVE
     // 获取周围角色信息
    float4 playerPosWS = GetTrailObject(positionWS);
    float dist = distance(positionWS, playerPosWS);
    {    
        // 草的描点
        float3 pivotPointOS = SAMPLE_TEXTURE2D_LOD(_GrassPivotPointTex, sampler_GrassPivotPointTex, input.texcoord2, 0.0).xzy * _GrassPivotPointTexUnit;
        float3 pivotPointWS = TransformObjectToWorld(pivotPointOS);

        float pushDown = saturate((1 - dist / playerPosWS.w) * lerpY) * _GrassPushStrength;
        float3 direction = normalize(playerPosWS - pivotPointWS);
        float3 newPos = positionWS + (direction * pushDown);
	    float orgDist = distance(positionWS, pivotPointWS);
        positionWS = pivotPointWS + (normalize(newPos - pivotPointWS) * orgDist);
    }
#endif
    
    return positionWS;
}

inline void InitializeSurfaceData(Varyings input, out CustomSurfaceData surfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord);
#if USING_ALPHA_CUTOFF
    clip(albedoAlpha.a - _Cutoff);
#endif
    
    surfaceData = (CustomSurfaceData) 0;
#if USING_SOLID_COLOR
    surfaceData.albedo = lerp(_BaseColor, _GrassTipColor, input.texcoord.y * input.texcoord.y);
#else
    surfaceData.albedo = albedoAlpha.rgb * lerp(_BaseColor, _GrassTipColor, input.texcoord.y * input.texcoord.y);
#endif
    surfaceData.alpha = albedoAlpha.a;
}

inline void InitializeInputData(Varyings input, CustomSurfaceData surfaceData, out CustomInputData inputData)
{
    inputData = (CustomInputData) 0;
    inputData.positionWS = input.positionWSAndFog.xyz;
    inputData.normalWS = input.normalWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.viewDirectionWS = viewDirWS;

	// 阴影值
    inputData.shadowCoord = GetShadowCoordInFragment(inputData.positionWS, input.shadowCoord);
    
    // 雾
    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFog.w);
    
    inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS.xyz);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1, 1, 1, 1);
}

Varyings vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionWS = GetGrassPosition(input);

    Varyings output;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.texcoord = input.texcoord;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.positionWSAndFog = float4(positionWS, ComputeFogFactor(output.positionCS.z));
    output.shadowCoord = GetShadowCoord(positionWS, output.positionCS);

    // sh
    OUTPUT_SH(output.normalWS, output.vertexSH);
        
    return output;
}

FragData frag(Varyings input)
{
    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, surfaceData);

    CustomInputData inputData;
    InitializeInputData(input, surfaceData, inputData);
    
    half4 shadowMask = CalculateShadowMask(inputData);
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    BxDFContext bxdfContext = GetBxDFContext(inputData, mainLight.direction);
    
    // 阴影值
    half shadowAtten = MainLightRealtimeShadow(inputData.shadowCoord);
    half3 shadow = lerp(_GrassShadowColor, 1, shadowAtten);

    // GI
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    inputData.bakedGI *= surfaceData.albedo;
    half3 giColor = inputData.bakedGI;
    
    // 反射环境
    half perceptualRoughness = _Roughness;
    giColor += _ReflectionIntensity * GetGlossyEnvironmentReflection(bxdfContext.R, perceptualRoughness);

    // 主灯颜色(草不要暗部效果)
    half3 lightColor = surfaceData.albedo * mainLight.color * shadow * bxdfContext.NoL_01;
    
    half3 color = lightColor + giColor;

    // 与雾混合
    color = MixFog(color, inputData.fogCoord);

    FragData output = (FragData) 0;
    output.color = half4(color, 1);
    output.normal = float4(input.normalWS * 0.5 + 0.5, 0.0);
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
    
    float3 positionWS = GetGrassPosition(input);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
#if USING_ALPHA_CUTOFF
    output.texcoord = input.texcoord;
#endif

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - vertexInput.positionWS);
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
    clip(albedoAlpha.a - _Cutoff);
#endif
    return 0;
}

///////////////////////////////////////////////////////////////////////////////
//                              DepthOnly                                     /
///////////////////////////////////////////////////////////////////////////////
Varyings DepthOnlyVertex(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float3 positionWS = GetGrassPosition(input);
    
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
#if USING_ALPHA_CUTOFF
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord.xy);
	clip(albedoAlpha.a - _Cutoff);
#endif
    return 0;
}

#endif
