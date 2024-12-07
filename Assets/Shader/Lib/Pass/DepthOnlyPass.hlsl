#ifndef __DEPTH_ONLY_PASS_HLSL__
#define __DEPTH_ONLY_PASS_HLSL__

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

struct Attributes
{
    float4 positionOS   : POSITION;
#if USING_ALPHA_CUTOFF
    float2 texcoord     : TEXCOORD0;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
#if USING_ALPHA_CUTOFF
    float2 texcoord     : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};
    
#include "../Instancing.hlsl"

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
#if USING_ALPHA_CUTOFF
    output.texcoord = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if USING_ALPHA_CUTOFF
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.texcoord.xy);
	clip(albedoAlpha.a - _Cutoff);
#endif
    return 0;
}

#endif
