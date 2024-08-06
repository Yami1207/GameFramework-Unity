#ifndef __SHADOW_CASTER_PASS_HLSL__
#define __SHADOW_CASTER_PASS_HLSL__

// ========================= 开关定义 =========================
#define USING_ALPHA_CUTOFF (USE_ALPHA_CUTOFF)

#include "../Core.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
#if USING_ALPHA_CUTOFF
float4 _BaseMap_ST;
half _Cutoff;
#endif
CBUFFER_END

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
#if USING_ALPHA_CUTOFF
    float2 texcoord     : TEXCOORD0;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
#if USING_ALPHA_CUTOFF
    half2 texcoord      : TEXCOORD0;
#endif
};

#include "../Instancing.hlsl"

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = SafeNormalize(_LightPosition - vertexInput.positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalInput.normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    output.positionCS = positionCS;
#if USING_ALPHA_CUTOFF
    output.texcoord = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
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

#endif
