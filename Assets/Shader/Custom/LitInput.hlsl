#ifndef __LIT_INPUT_HLSL__
#define __LIT_INPUT_HLSL__

// ========================= 开关定义 =========================
// 法线贴图
#define USING_BUMP_MAP (USE_BUMP_MAP) 

#include "../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
uniform half3 _BaseColor;
uniform half _BumpScale;

uniform half _Metallic;
uniform half _Smoothness;

uniform half _EmissionIntensity;
uniform half3 _EmissionColor;

//--------------------------------------
// 贴图
//TEXTURE2D(_BumpMap);
//SAMPLER(sampler_BumpMap);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    half2 texcoord      : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
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
    float4 shadowCoord      : TEXCOORD4;
    half4 vertexSHAndFog    : TEXCOORD5; // xyz: sh9 w: fog
    
    UNITY_VERTEX_OUTPUT_STEREO
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
    half4 color     : SV_Target0;
    float4 normal   : SV_Target1;
};

#include "../Lib/Instancing.hlsl"

#endif
