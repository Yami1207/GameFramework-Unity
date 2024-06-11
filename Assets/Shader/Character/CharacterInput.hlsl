#ifndef __CHARACTER_INPUT_HLSL__
#define __CHARACTER_INPUT_HLSL__

// ========================= 开关定义 =========================
// 法线贴图
#define USING_BUMP_MAP (USE_BUMP_MAP) 

#include "../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half3 _SpecularColor;
    half3 _EmissionColor;
CBUFFER_END

//--------------------------------------
// 贴图
TEXTURE2D(_AlbedoTex);
SAMPLER(sampler_AlbedoTex);

TEXTURE2D(_MaskTex);
SAMPLER(sampler_MaskTex);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    half2 texcoord      : TEXCOORD0;
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half2 texcoord          : TEXCOORD0;
#if USING_BUMP_MAP
    float4 tangentWS        : TEXCOORD1;
    float4 bitangentWS      : TEXCOORD2;
    float4 normalWS         : TEXCOORD3;
#else
    float3 positionWS       : TEXCOORD1;
    float3 normalWS         : TEXCOORD2;
#endif
    half4 fogFactorAndSH9   : TEXCOORD4;    // xyz: sh9 w: fog
    
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord		: TEXCOORD5;
#endif
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
    half4 color     : SV_Target0;
    float4 normal   : SV_Target1;
};

#endif
