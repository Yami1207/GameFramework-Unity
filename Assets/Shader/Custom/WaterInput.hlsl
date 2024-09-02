#ifndef __WATER_INPUT_HLSL__
#define __WATER_INPUT_HLSL__

#include "../Lib/Core.hlsl"
#include "../Lib/Utils/CameraOpaqueTexture.hlsl"

// ========================= 开关定义 =========================
// 泡沫开关
#define USING_FOAM (_USE_FOAM)

// 交界处泡沫开关
#define USING_INTERSECTION (_USE_INTERSECTION)

// 反射开关
#define USING_REFLECTION (_USE_REFLECTION)

// 折射开关
#define USING_REFRACTION (_USE_REFRACTION)

// 高光开关
#define USING_SPECULAR (_USE_SPECULAR)

//--------------------------------------
// 材质属性
uniform half _DepthDistance;
uniform half _TransparentDistance;

uniform half2 _WaterDirection;
uniform half _WaterSpeed;

uniform half4 _WaterPhase;
uniform half4 _WaterAmplitude;
uniform half4 _WaterFrequency;
uniform half4 _WaterOffset;

uniform half4 _NormalTiling;
uniform half _NormalSubTiling;
uniform half _NormalSpeed;
uniform half _NormalSubSpeed;

uniform half3 _ReflectionColor;
uniform half _ReflectionDistort;
uniform half _ReflectionIntensity;

uniform half _RefractionFactor;

uniform half3 _SpecularColor;
uniform half _SpecularShinness;
uniform half _SpecularIntensity;

uniform half3 _FoamColor;
uniform half _FoamAmount;
uniform half4 _FoamTiling;
uniform half _FoamSubTiling;
uniform half _FoamSpeed;
uniform half _FoamSubSpeed;
uniform half _FoamDistortion;

uniform half _IntersectionClipping;
uniform half _IntersectionDistance;
uniform half3 _IntersectionColor;
uniform half _IntersectionThreshold;
uniform half _IntersectionTiling;
uniform half _IntersectionSpeed;
uniform half _IntersectionDistortion;
uniform half _IntersectionRippleStrength;

//--------------------------------------
// 贴图
TEXTURECUBE(_ReflectionCubemap);
SAMPLER(sampler_ReflectionCubemap);

TEXTURE2D(_FoamMaskMap);
SAMPLER(sampler_FoamMaskMap);

TEXTURE2D(_IntersectionNoiseMap);
SAMPLER(sampler_IntersectionNoiseMap);

//--------------------------------------
// 顶点结构体
struct Attributes
{
    float4 positionOS   : POSITION;
    half2 texcoord      : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

//--------------------------------------
// 片元结构体
struct Varyings
{
    float4 positionCS       : SV_POSITION;
    float4 positionWSAndFog : TEXCOORD0;
    float4 positionSS       : TEXCOORD1;
    half2 texcoord          : TEXCOORD2;
    half3 normalWS          : TEXCOORD3;
    float4 shadowCoord      : TEXCOORD4;
    half3 vertexSH          : TEXCOORD5;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

#include "../Lib/Instancing.hlsl"

#endif
