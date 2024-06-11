#ifndef __CHARACTER_PASS_HLSL__
#define __CHARACTER_PASS_HLSL__

#include "CharacterInput.hlsl"

inline void InitializeSurfaceData(Varyings input, out CustomSurfaceData surfaceData)
{
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.texcoord);
    half4 maskColor = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.texcoord.xy);
    
    surfaceData = (CustomSurfaceData) 0;
    surfaceData.albedo = albedoAlpha.rgb;
    surfaceData.alpha = albedoAlpha.a;
    
    surfaceData.specular = _SpecularColor;
    
    surfaceData.emission = _EmissionColor;
}

inline half3 FragmentShading(Varyings input, CustomSurfaceData surfaceData)
{
    return surfaceData.albedo;
}

Varyings vert(Attributes input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
#if USING_BUMP_MAP
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
#else
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
#endif
    
    Varyings output = (Varyings) 0;
    output.positionCS = vertexInput.positionCS;
    output.texcoord = input.texcoord;
    
#if USING_BUMP_MAP
	output.normalWS = float4(normalInput.normalWS, vertexInput.positionWS.x);
	output.tangentWS = float4(normalInput.tangentWS, vertexInput.positionWS.y);
	output.bitangentWS = float4(normalInput.bitangentWS, vertexInput.positionWS.z);
#else
    output.positionWS = vertexInput.positionWS;
    output.normalWS = normalInput.normalWS;
#endif
    
    // sh与雾
    OUTPUT_SH(normalInput.normalWS, output.fogFactorAndSH9);
    output.fogFactorAndSH9.w = ComputeFogFactor(vertexInput.positionCS.z);
    
    	// 阴影
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    
    return output;
}

FragData frag(Varyings input)
{
    CustomSurfaceData surfaceData;
    InitializeSurfaceData(input, surfaceData);
    
    half3 finalColor = FragmentShading(input, surfaceData);
    
    // 与雾混合
    finalColor = MixFog(finalColor, input.fogFactorAndSH9.a);
    
    FragData output = (FragData) 0;
    output.color = half4(finalColor, surfaceData.alpha);
    //output.normal = float4(input.normalWS * 0.5 + 0.5, 0.0);
    return output;
}

#endif
