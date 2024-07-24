#ifndef __GRASS_INPUT_HLSL__
#define __GRASS_INPUT_HLSL__

// ========================= 开关定义 =========================
#define USING_ALPHA_CUTOFF (_USE_ALPHA_CUTOFF)

#define USING_WIND (_ENABLE_WIND_ON)

#define USING_RIPPLING_WHEAT (_ENABLE_RIPPLING_WHEAT_ON)

#define USING_INTERACTIVE (_ENABLE_INTERACTIVE_ON)

#include "../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
uniform half3 _BaseColor;
uniform half3 _GrassTipColor;
uniform half3 _GrassShadowColor;
uniform half _AlphaCutoff;

uniform half _Metallic;
uniform half _Roughness;
uniform half _ReflectionIntensity;

uniform half _GrassPivotPointTexUnit;
uniform half _GrassPushStrength;

uniform half _RipplingWheatWaveSize;
uniform half _RipplingWheatWaveSpeed;

uniform half _EmissionIntensity;
uniform half3 _EmissionColor;

//--------------------------------------
// 贴图
TEXTURE2D(_RipplingWheatMap);
SAMPLER(sampler_RipplingWheatMap);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    half2 texcoord      : TEXCOORD0;
    float2 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    half3 texcoord          : TEXCOORD0;
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
    half4 color     : SV_Target0;
    float4 normal   : SV_Target1;
};

#include "../Lib/Instancing.hlsl"
#include "../Lib/Utils/Wind.hlsl"
#include "../Lib/Utils/ObjectTrails.hlsl"

#endif
