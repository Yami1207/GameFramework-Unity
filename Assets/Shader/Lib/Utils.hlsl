#ifndef __UTILS_HLSL__
#define __UTILS_HLSL__

inline float4 GetShadowCoord(float3 positionWS, float4 positionCS)
{
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    return ComputeScreenPos(positionCS);
#else
    return TransformWorldToShadowCoord(positionWS);
#endif
}

inline half4 GetShadowCoordInFragment(float3 positionWS, float4 shadowCoord)
{
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	return shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	return TransformWorldToShadowCoord(positionWS);
#else
    return float4(0, 0, 0, 0);
#endif
}

//--------------------------------------
// 模拟正弦波（代替sin）
// https://developer.nvidia.com/gpugems/gpugems3/part-iii-rendering/chapter-16-vegetation-procedural-animation-and-shading-crysis
inline float4 SmoothCurve(float4 x)
{
    return x * x * (3.0 - 2.0 * x);
}

inline float4 TriangleWave(float4 x)
{
    return abs(frac(x + 0.5) * 2.0 - 1.0);
}

inline float4 SmoothTriangleWave(float4 x)
{
    return SmoothCurve(TriangleWave(x));
}

// 获取旋转矩阵
inline float3x3 MatrixRotate(float3 axis, float radian)
{
    float s, c;
    sincos(radian, s, c);
    float t = 1.f - c;

    float tx = t * axis.x;
    float ty = t * axis.y;
    float tz = t * axis.z;

    float sx = s * axis.x;
    float sy = s * axis.y;
    float sz = s * axis.z;

    float3x3 mat;
    mat._m00 = tx * axis.x + c;
    mat._m01 = tx * axis.y + sz;
    mat._m02 = tx * axis.z - sy;

    mat._m10 = tx * axis.y - sz;
    mat._m11 = ty * axis.y + c;
    mat._m12 = ty * axis.z + sx;

    mat._m20 = tx * axis.z + sy;
    mat._m21 = ty * axis.z - sx;
    mat._m22 = tz * axis.z + c;

    return mat;
}

inline float3 RotateAboutAxis(float3 position, float3 axis, float radian)
{
    return mul(MatrixRotate(axis, radian), position);
}

inline float3 RotateAboutAxis(float4 rotationAxisAndAngle, float3 positionOnAxis, float3 position)
{
    float s, c;
    sincos(ANGLE_TO_RADIAN(rotationAxisAndAngle.w), s, c);
    
    // Project Position onto the rotation axis and find the closest point on the axis to Position
    float3 ClosestPointOnAxis = positionOnAxis + rotationAxisAndAngle.xyz * dot(rotationAxisAndAngle.xyz, position - positionOnAxis);
	// Construct orthogonal axes in the plane of the rotation
    float3 UAxis = position - ClosestPointOnAxis;
    float3 VAxis = cross(rotationAxisAndAngle.xyz, UAxis);

    // Rotate using the orthogonal axes
    float3 R = UAxis * c + VAxis * s;
	// Reconstruct the rotated world space position
    float3 RotatedPosition = ClosestPointOnAxis + R;
    // Convert from position to a position offset
    return RotatedPosition - position;
}

// 获取渐变色(https://sp4ghet.github.io/grad/)
inline half4 CosineGradient(float x, half4 phase, half4 amp, half4 freq, half4 offset)
{
    x *= TWO_PI;
    phase *= TWO_PI;
    return half4(offset + 0.5 * amp * cos(x * freq + phase) + 0.5);
}

#endif
