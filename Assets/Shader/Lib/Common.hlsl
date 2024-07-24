#ifndef __COMMON_HLSL__
#define __COMMON_HLSL__

#define ANGLE_TO_RADIAN(x) 0.0174532924 * x

inline half Max3(half3 x)
{
    return max(x.x, max(x.y, x.z));
}

inline half Min3(half3 x)
{
    return min(x.x, min(x.y, x.z));
}

inline half Min3(half x, half y, half z)
{
    return min(x, min(y, z));
}

inline half4 Min3(half4 x, half4 y, half4 z)
{
    return min(x, min(y, z));
}

inline float Pow2(float x)
{
    return x * x;
}

inline float Pow3(float x)
{
    return x * x * x;
}

//inline float Pow4(float x)
//{
//    return (x * x) * (x * x);
//}

inline float Pow5(float x)
{
    return x * x * x * x * x;
}

// https://colinbarrebrisebois.com/2012/04/09/approximating-translucency-revisited-with-simplified-spherical-gaussian/
inline float PowOptimize(float x, float n)
{
    n = n * 1.4427f + 1.4427f; // 1.4427f --> 1/ln(2)
    return exp2(x * n - n);
}

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
