#ifndef __CHARACTER_INPUT_HLSL__
#define __CHARACTER_INPUT_HLSL__

// ========================= 开关定义 =========================
#define USING_MATERIAL_VALUES_PACK_LUT (_USE_MATERIAL_VALUES_PACK_LUT)

#define USING_SPECULAR (_USE_SPECULAR)

#define USING_RIM_LIGHT (_USE_RIM_LIGHT)

#define USING_RIM_SHADOW (_USE_RIM_SHADOW)

#define USING_EMISSION (_USE_EMISSION)

#include "CharacterFunctions.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _BackColor;
    half4 _SpecularColor;
    half4 _RimColor;
    half4 _RimShadowColor;
    half4 _EmissionColor;

#if defined(CHARACTER_FACE_PASS)
    half4 _ShadowColor;
    half4 _EyeShadowColor;
    //half4 _NoseLineColor;
    half4 _ExCheekColor;
    half4 _ExShyColor;
    half4 _ExShadowColor;
    half4 _ExEyeShadowColor;
#endif

    // 高光
    half _SpecularIntensity;
    half _SpecularShininess;
    half _SpecularRoughness;

    // 边缘光
    half _RimWidth;
    half _RimLightThreshold;
    half _RimLightEdgeSoftness;

    // Rim Shadow
    half _RimShadowIntensity;
    half _RimShadowWidth;
    half _RimShadowFeather;

    // 自发光
    half _EmissionThreshold;
    half _EmissionIntensity;

#if defined(CHARACTER_FACE_PASS)
    //half _NoseLinePower;
    half _ExCheekIntensity;
    half _ExShyIntensity;
    half _ExShadowIntensity;
#endif
CBUFFER_END

//--------------------------------------
// 贴图
TEXTURE2D(_AlbedoTex);
SAMPLER(sampler_AlbedoTex);

// r:边缘光宽度 g:ao b:高光阈值 a:rampLevel
TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);

TEXTURE2D(_CoolRampTex);
SAMPLER(sampler_CoolRampTex);

TEXTURE2D(_WarmRampTex);
SAMPLER(sampler_WarmRampTex);

TEXTURE2D(_FaceTex);
SAMPLER(sampler_FaceTex);

TEXTURE2D(_FaceExpressionTex);
SAMPLER(sampler_FaceExpressionTex);

TEXTURE2D(_MaterialValuesPackLUT);
SAMPLER(sampler_MaterialValuesPackLUT);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    half2 texcoord0     : TEXCOORD0;
    half2 texcoord1     : TEXCOORD1;
    half4 color         : COLOR;
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half4 texcoord          : TEXCOORD0;
    float3 positionWS       : TEXCOORD1;
    float3 normalWS         : TEXCOORD2;
    half4 color             : TEXCOORD3;
    half4 fogFactorAndSH9   : TEXCOORD4; // xyz: sh9 w: fog
	float4 shadowCoord		: TEXCOORD5;
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
    half4 color : SV_Target0;
    float4 normal : SV_Target1;
};

inline half3 SampleCoolRampColor(half2 uv)
{
    return SAMPLE_TEXTURE2D(_CoolRampTex, sampler_CoolRampTex, uv).rgb;
}

inline half3 SampleWarmRampColor(half2 uv)
{
    return SAMPLE_TEXTURE2D(_WarmRampTex, sampler_WarmRampTex, uv).rgb;
}

#endif
