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

// 
inline float3x3 MatrixRotate(float radian, float3 axis)
{
    float s, c, t;
    float tx, ty, tz;
    float sx, sy, sz;

    //s = sin(radian);
    //c = cos(radian);
    sincos(radian, s, c);
    t = 1.f - c;

    tx = t * axis.x;
    ty = t * axis.y;
    tz = t * axis.z;

    sx = s * axis.x;
    sy = s * axis.y;
    sz = s * axis.z;

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

inline float3 RotateAboutAxis(float4 NormalizedRotationAxisAndAngle, float3 PositionOnAxis, float3 Position)
{
			// Project Position onto the rotation axis and find the closest point on the axis to Position
    float3 ClosestPointOnAxis = PositionOnAxis + NormalizedRotationAxisAndAngle.xyz * dot(NormalizedRotationAxisAndAngle.xyz, Position - PositionOnAxis);
			// Construct orthogonal axes in the plane of the rotation
    float3 UAxis = Position - ClosestPointOnAxis;
    float3 VAxis = cross(NormalizedRotationAxisAndAngle.xyz, UAxis);
    float CosAngle;
    float SinAngle;
    sincos(NormalizedRotationAxisAndAngle.w, SinAngle, CosAngle);
			// Rotate using the orthogonal axes
    float3 R = UAxis * CosAngle + VAxis * SinAngle;
			// Reconstruct the rotated world space position
    float3 RotatedPosition = ClosestPointOnAxis + R;
			// Convert from position to a position offset
    return RotatedPosition - Position;
}

#endif
