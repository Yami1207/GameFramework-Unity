#ifndef __IBL_SPECULAR_HLSL__
#define __IBL_SPECULAR_HLSL__

//////////////////////////////////////////////
// Image Based Lighting

// 环境反射
half3 GetGlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, TEXTURECUBE_PARAM(tex, sampler_tex), half4 hdr)
{
	half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness, 8);
	half4 encodedIrradiance = G2L(SAMPLE_TEXTURECUBE_LOD(tex, sampler_tex, reflectVector, mip));

	half3 irradiance = 0;
#if defined(UNITY_USE_NATIVE_HDR)
	irradiance = encodedIrradiance.rgb;
#else
	irradiance = DecodeHDREnvironment(encodedIrradiance, hdr);
#endif

	return irradiance;
}

half3 GetGlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness)
{
    return GetGlossyEnvironmentReflection(reflectVector, perceptualRoughness, TEXTURECUBE_ARGS(unity_SpecCube0, samplerunity_SpecCube0), unity_SpecCube0_HDR);
}

#endif
