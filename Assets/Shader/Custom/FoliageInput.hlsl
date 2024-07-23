#ifndef __FOLIAGE_INPUT_HLSL__
#define __FOLIAGE_INPUT_HLSL__

// ========================= 开关定义 =========================
#define USING_HALF_LAMBERT (USE_HALF_LAMBERT)

#define USING_GRADIENT_COLOR (_USE_GRADIENT_COLOR)

#define USING_ALPHA_CUTOFF (_USE_ALPHA_CUTOFF)

#define USING_SSS (USE_SSS)

#define USING_WIND (_ENABLE_WIND_ON)

#include "../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
uniform half3 _BaseColor;
uniform half3 _BaseBottomColor;
uniform half _ColorMaskHeight;

uniform half _AlphaCutoff;

uniform half _SubsurfaceRadius;
uniform half3 _SubsurfaceColor;
uniform half _SubsurfaceColorIntensity;

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    half2 texcoord      : TEXCOORD0;
    half4 color         : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half2 texcoord          : TEXCOORD0;
    half4 color             : TEXCOORD1;
    float3 positionWS       : TEXCOORD2;
    float3 normalWS         : TEXCOORD3;

    float4 shadowCoord      : TEXCOORD4;
    half4 vertexSHAndFog    : TEXCOORD5; // xyz: sh9 w: fog
    
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
#include "../Lib/Utils/Wind.hlsl"

#endif
