#ifndef __WIND_HLSL_H__
#define __WIND_HLSL_H__

// xy: 风速 z: 强度
uniform half4 _g_WindParameter;

inline float GetWindIntensity()
{
    return _g_WindParameter.z;
}

inline float3 SimpleSwingPosOS(float3 positionOS, float frequency, float amplitude, float stiffness, float phase, float windIntensity)
{
    float len = length(positionOS);
    float sinWave = 2 * SmoothTriangleWave(_Time.y * frequency + phase).x - 1;
    float2 offsetOS = sinWave * amplitude * _g_WindParameter.xy;
    positionOS.xz += offsetOS * stiffness * windIntensity;

    float3 newPositionOS = normalize(positionOS) * len;
    return lerp(newPositionOS, positionOS, 0.3);
}

inline float3 SimpleSwingPosOS(float3 newPositionOS, float frequency, float amplitude, float stiffness, float phase)
{
    return SimpleSwingPosOS(newPositionOS, frequency, amplitude, stiffness, phase, GetWindIntensity());
}

#endif
