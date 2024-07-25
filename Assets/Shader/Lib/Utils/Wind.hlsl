#ifndef __WIND_HLSL_H__
#define __WIND_HLSL_H__

#include "Noise.hlsl"

uniform half4 _G_WindParameter;

// 风方向
#define WIND_DIRECTION  _G_WindParameter.xy

// 风速
#define WIND_SPEED      _G_WindParameter.z

// 风强度
#define WIND_INTENSITY  _G_WindParameter.w

// 风浪参数(x: 范围 y: 1 / 强度 z:强度)
uniform half4 _G_WindWavePrams;

// 风浪纹理
TEXTURE2D(_G_WindWaveMap);
SAMPLER(sampler_G_WindWaveMap);

inline float3 SimpleGrassWind(float3 positionWS, float weight)
{
    float speed = _Time.y * WIND_SPEED;
    float noise = fbm(positionWS.xz + WIND_DIRECTION * speed, 3);
    return weight * noise * WIND_INTENSITY;
}

inline float4 SimpleWindWave(float3 positionWS, float weight)
{
    float2 uv = positionWS.xz * _G_WindWavePrams.y + _Time.x * WIND_SPEED * WIND_DIRECTION;
    float mask = 1 - SAMPLE_TEXTURE2D_LOD(_G_WindWaveMap, sampler_G_WindWaveMap, uv, 0).x;
    float wave = Pow2(mask * weight);
    float3 offset = sin(0.1 * wave) * WIND_SPEED * weight * _G_WindWavePrams.z;
    return float4(offset, wave);
}

//inline float3 SimpleSwingPositionOS(float3 positionOS, float frequency, float amplitude, float stiffness, float phase, float windIntensity)
//{
//    float len = length(positionOS);
//    float sinWave = 2 * SmoothTriangleWave(_Time.y * frequency + phase).x - 1;
//    float2 offsetOS = sinWave * amplitude * _G_WindParameter.xy;
//    positionOS.xz += offsetOS * stiffness * windIntensity;

//    float3 newPositionOS = normalize(positionOS) * len;
//    return lerp(newPositionOS, positionOS, 0.3);
//}

//inline float3 SimpleSwingPositionOS(float3 positionOS, float frequency, float amplitude, float stiffness, float phase)
//{
//    return SimpleSwingPositionOS(positionOS, frequency, amplitude, stiffness, phase, GetWindIntensity());
//}

#endif
