#ifndef __TERRAIN_INPUT_HLSL__
#define __TERRAIN_INPUT_HLSL__

// ========================= 开关定义 =========================
#define PIXEL_DEPTH_OFFSET_PASS defined(_RENDER_PIXEL_DEPTH_OFFSET)

#include "../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half4 _TerrianParam;
    half4 _Splat0_ST;
    half4 _Splat1_ST;
    half4 _Splat2_ST;
    half4 _Splat3_ST;
    half _NormalScale0;
    half _NormalScale1;
    half _NormalScale2;
    half _NormalScale3;
    half _Metallic0;
    half _Metallic1;
    half _Metallic2;
    half _Metallic3;
    half _Smoothness0;
    half _Smoothness1;
    half _Smoothness2;
    half _Smoothness3;
CBUFFER_END

//--------------------------------------
// 贴图
TEXTURE2D(_Control);
SAMPLER(sampler_Control);

TEXTURE2D(_HeightMap);
SAMPLER(sampler_HeightMap);

TEXTURE2D(_VertexNormalMap);
SAMPLER(sampler_VertexNormalMap);

TEXTURE2D(_Splat0);
SAMPLER(sampler_Splat0);

TEXTURE2D(_Splat1);
SAMPLER(sampler_Splat1);

TEXTURE2D(_Splat2);
SAMPLER(sampler_Splat2);

TEXTURE2D(_Splat3);
SAMPLER(sampler_Splat3);

TEXTURE2D(_Normal0);
SAMPLER(sampler_Normal0);

TEXTURE2D(_Normal1);
SAMPLER(sampler_Normal1);

TEXTURE2D(_Normal2);
SAMPLER(sampler_Normal2);

TEXTURE2D(_Normal3);
SAMPLER(sampler_Normal3);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionWSAndFog : TEXCOORD0;
    float2 texcoord : TEXCOORD1;
    float3 tangentWS : TEXCOORD2;
    float3 bitangentWS : TEXCOORD3;
    float3 normalWS : TEXCOORD4;
    float4 shadowCoord : TEXCOORD5;
    half3 vertexSH : TEXCOORD6;

    UNITY_VERTEX_OUTPUT_STEREO
};

//--------------------------------------
// 片元输出结构体
struct FragData
{
#if PIXEL_DEPTH_OFFSET_PASS
    float4 color : SV_Target0;
#else
    half4 color : SV_Target0;
#endif
    float4 normal : SV_Target1;
};

#include "../Lib/Instancing.hlsl"

#endif
