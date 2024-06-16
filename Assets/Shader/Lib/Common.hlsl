#ifndef __COMMON_HLSL__
#define __COMMON_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

inline half4 G2L(half4 color)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return SRGBToLinear(color);
	//return half4(GammaToLinearSpace(color.rgb), color.a);
#else
    return color;
#endif
}

inline half3 G2L(half3 color)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return SRGBToLinear(color);
	//return GammaToLinearSpace(color);
#else
    return color;
#endif
}

inline half4 L2G(half4 color)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return LinearToSRGB(color);
	//return half4(LinearToGammaSpace(color.rgb), color.a);
#else
    return color;
#endif
}

inline half3 L2G(half3 color)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return LinearToSRGB(color);
	//return LinearToGammaSpace(color);
#else
    return color;
#endif
}

#endif
