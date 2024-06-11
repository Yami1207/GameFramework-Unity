#ifndef __DEPTH_ONLY_PASS_HLSL__
#define __DEPTH_ONLY_PASS_HLSL__

#include "../../Lib/Core.hlsl"

//--------------------------------------
// 材质属性
CBUFFER_START(UnityPerMaterial)
    half4 _TerrianParam;
CBUFFER_END

TEXTURE2D(_HeightMap);
SAMPLER(sampler_HeightMap);

struct Attributes
{
    float4 positionOS : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};
    
#include "../../Lib/Instancing.hlsl"
    
Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings) 0;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    UNITY_SETUP_INSTANCE_ID(input);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float2 texcoord = (positionWS.xz - _TerrianParam.zw) / _TerrianParam.xy;
    positionWS.y = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, texcoord, 0.0).r;
    output.positionCS = TransformWorldToHClip(positionWS);
    return output;
}

    half4 DepthOnlyFragment(Varyings input) : SV_TARGET
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        return 0;
    }

#endif
